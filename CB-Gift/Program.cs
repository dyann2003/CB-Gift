using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Hubs;
using CB_Gift.Jobs;
using CB_Gift.Orders.Import;
using CB_Gift.Services;
using CB_Gift.Services.Email;
using CB_Gift.Services.IService;
using FluentValidation;
using CB_Gift.Services.Payments;
using CB_Gift.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Quartz;
using System.Text;
using CB_Gift.Services.Reports;

var builder = WebApplication.CreateBuilder(args);

// ================== 1. Controllers + Swagger ==================
builder.Services.AddControllers()
    .AddJsonOptions(x =>
        x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CBGift API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// ================== 2. DbContext ==================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<CBGiftDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

// ================== 3. Identity & Cookie Config ==================
builder.Services
    .AddIdentityCore<AppUser>(opt =>
    {
        opt.User.RequireUniqueEmail = true;
        opt.Password.RequiredLength = 6;
        opt.Password.RequireNonAlphanumeric = false;
        opt.Password.RequireUppercase = false;
        opt.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<CBGiftDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Cấu hình Cookie cho Identity để chạy được Cross-Domain (Vercel -> DigitalOcean)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.None; // Cho phép cookie đi qua domain khác
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Bắt buộc phải có HTTPS (DigitalOcean cung cấp)
    options.Cookie.HttpOnly = true;              // Bảo mật
    options.Cookie.Name = ".CBGift.Auth";        // Đặt tên cho dễ nhận diện
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

builder.Services.AddAutoMapper(typeof(Program));

// ================== 4. JWT Authentication ==================
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ClockSkew = TimeSpan.Zero
        };

        // Logic tự động lấy Token từ Cookie hoặc Query (cho SignalR)
        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                // ================== CƠ CHẾ HYBRID ==================

                // 1. Ưu tiên cao nhất: Lấy Token từ Header (Dành cho Mobile App / Postman)
                // Mobile gửi lên dạng: "Authorization: Bearer eyJhbGci..."
                string authorization = ctx.Request.Headers["Authorization"];
                if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Token = authorization.Substring("Bearer ".Length).Trim();
                }

                // 2. Ưu tiên nhì: Lấy từ Query String (Dành cho SignalR - NotificationHub)
                // SignalR không gửi Header được khi kết nối WebSocket ban đầu
                else if (!string.IsNullOrEmpty(ctx.Request.Query["access_token"]))
                {
                    var accessToken = ctx.Request.Query["access_token"];
                    var path = ctx.HttpContext.Request.Path;

                    // Chỉ chấp nhận token qua Query nếu đường dẫn là Hub
                    if (!string.IsNullOrEmpty(path) && path.StartsWithSegments("/notificationHub"))
                    {
                        ctx.Token = accessToken;
                    }
                }

                // 3. Ưu tiên cuối: Lấy từ Cookie (Dành cho Web Desktop / Next.js)
                // Nếu không có Header, không có Query -> Tự động tìm trong Cookie
                else if (ctx.Request.Cookies.TryGetValue("access_token", out var cookieToken))
                {
                    ctx.Token = cookieToken;
                }

                return Task.CompletedTask;
            }
        };
    });

// ================== 5. GHN Settings ==================
var ghnSettings = builder.Configuration.GetSection("GhnSettings");
var ghnToken = ghnSettings["Token"];
var ghnProdBaseUrl = ghnSettings["ProdBaseAddress"];
var ghnTokenDev = ghnSettings["TokenDev"];
var ghnDevBaseUrl = ghnSettings["DevBaseAddress"];
var ghnShopId = ghnSettings["ShopId"];

// Validation GHN Config
if (string.IsNullOrEmpty(ghnToken) || string.IsNullOrEmpty(ghnProdBaseUrl))
    throw new InvalidOperationException("Lỗi cấu hình: GhnSettings:Token hoặc ProdBaseAddress chưa được đặt.");

builder.Services.AddAuthorization();

builder.Services.AddHttpClient("GhnProdClient", client =>
{
    client.BaseAddress = new Uri(ghnProdBaseUrl);
    client.DefaultRequestHeaders.Add("Token", ghnToken);
    client.DefaultRequestHeaders.Add("ShopId", ghnShopId);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient("GhnDevClient", client =>
{
    client.BaseAddress = new Uri(ghnDevBaseUrl);
    client.DefaultRequestHeaders.Add("Token", ghnTokenDev);
    client.DefaultRequestHeaders.Add("ShopId", ghnShopId);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// ================== 6. CORS (Cấu hình domain Vercel) ==================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000",
            "http://localhost:5173", // Vite Local
            "https://cb-gift-fe-sby6-mazut4syf-bachquangles-projects.vercel.app",
            "https://cb-gift-fe-sby6.vercel.app"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials(); // Bắt buộc để nhận Cookie/Token
    });
});

// ================== 7. Register Services (DI) ==================
builder.Services.AddScoped<GhnShippingService>();

var useDemoMode = builder.Configuration.GetValue<bool>("GhnSettings:UseDemoMode");
if (useDemoMode)
{
    Console.WriteLine(">>> HỆ THỐNG ĐANG CHẠY CHẾ ĐỘ DEMO SHIPMENT <<<");
    builder.Services.AddScoped<IShippingService, DemoShippingService>();
}
else
{
    Console.WriteLine(">>> HỆ THỐNG ĐANG CHẠY CHẾ ĐỘ REAL SHIPMENT <<<");
    builder.Services.AddScoped<IShippingService>(provider => provider.GetRequiredService<GhnShippingService>());
}

builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));

builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IManagementAccountService, ManagementAccountService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<IOrderDetailService, OrderDetailService>();
builder.Services.AddScoped<IPlanService, PlanService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.AddScoped<IImageManagementService, ImageManagementService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IDesignerSellerService, DesignerSellerService>();
builder.Services.AddScoped<IDesignerTaskService, DesignerTaskService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IAiStudioService, AiStudioService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ICancellationService, CancellationService>();
builder.Services.AddScoped<IRefundService, RefundService>();
builder.Services.AddScoped<IReprintService, ReprintService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<PayOSService>();
builder.Services.AddScoped<VNPayService>();
builder.Services.AddScoped<PaymentGatewayFactory>();
builder.Services.AddScoped<VnPayHelper>();
builder.Services.AddScoped<ILocationService, GhnLocationService>();
builder.Services.AddScoped<IGhnPrintService, GhnPrintService>();
builder.Services.AddSingleton<NotificationHub>();
builder.Services.AddScoped<OrderFactory>();
builder.Services.AddScoped<ReferenceDataCache>();
builder.Services.AddScoped<IValidator<OrderImportRowDto>, OrderImportRowValidator>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IOrderImportService, OrderImportService>();

// ================== 8. Quartz ==================
builder.Services.AddQuartz(q =>
{
    q.SchedulerId = "Scheduler-Core";
    var jobKey = new JobKey("groupOrdersJob");
    q.AddJob<GroupOrdersJob>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("groupOrdersTrigger")
        .StartNow()
        .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(0, 5))
    );

    var invoiceJobKey = new JobKey("createMonthlyInvoicesJob");
    q.AddJob<CreateMonthlyInvoicesJob>(opts => opts.WithIdentity(invoiceJobKey));
    q.AddTrigger(opts => opts
        .ForJob(invoiceJobKey)
        .WithIdentity("monthlyInvoiceTrigger")
        .StartNow()
        .WithSchedule(CronScheduleBuilder.MonthlyOnDayAndHourAndMinute(10, 0, 5))
    );
});
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

// Cấu hình nhận diện Header từ Proxy của Digital Ocean
// Nếu không có đoạn này, App sẽ nghĩ nó đang chạy HTTP thường -> Cookie Secure bị hủy -> Lỗi 401
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Xóa giới hạn known networks/proxies để chấp nhận từ DO Load Balancer
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// ================== Middleware Pipeline ==================

// Phải đặt đầu tiên hoặc ngay sau Build
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Trên Production nên dùng cái này để ép HTTPS nếu muốn
    app.UseHsts();
}

app.UseHttpsRedirection();

// Thứ tự Middleware chuẩn: ForwardedHeaders -> CORS -> Auth -> Logic
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/notificationHub");

// ================== Seed Data ==================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await SeedRolesAsync(services);
        await SeedAllDataAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi xảy ra khi Seeding dữ liệu.");
    }
}

app.Run();

// ================== Seed Functions ==================
static async Task SeedRolesAsync(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = { "Seller", "Designer", "QC", "Staff", "Manager" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

static async Task SeedAllDataAsync(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

    string[] roleNames = { "Seller", "Designer", "QC", "Staff", "Manager" };

    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    var usersToSeed = new List<(string Email, string Password, string Role)>
    {
        ("seller@example.com", "Seller@123", "Seller"),
        ("designer@example.com", "Designer@123", "Designer"),
        ("qc@example.com", "Qc@123", "QC"),
        ("staff@example.com", "Staff@123", "Staff"),
        ("manager@example.com", "Manager@123", "Manager")
    };

    foreach (var userSpec in usersToSeed)
    {
        var user = await userManager.FindByEmailAsync(userSpec.Email);
        if (user == null)
        {
            var newUser = new AppUser
            {
                UserName = userSpec.Email,
                Email = userSpec.Email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(newUser, userSpec.Password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(newUser, userSpec.Role);
            }
        }
    }
}