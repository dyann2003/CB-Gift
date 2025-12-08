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
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ================== 2. DbContext ==================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<CBGiftDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

// ================== 3. Identity ==================
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

builder.Services.AddAutoMapper(typeof(Program));

// ================== 4. JWT (Quan trọng cho Cookie) ==================
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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ClockSkew = TimeSpan.Zero // Hết hạn là chặn ngay
        };

        // Logic tự động lấy Token từ Cookie cho mỗi Request
        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                // 1. Ưu tiên lấy từ Query String (cho SignalR)
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/notificationHub"))
                {
                    ctx.Token = accessToken;
                }
                // 2. Nếu không có query, lấy từ Cookie "access_token"
                // Đây là chỗ giúp [Authorize] hoạt động mà không cần header Authorization
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

if (string.IsNullOrEmpty(ghnToken) || string.IsNullOrEmpty(ghnProdBaseUrl))
    throw new InvalidOperationException("Lỗi cấu hình: GhnSettings:Token hoặc ProdBaseAddress chưa được đặt.");
if (string.IsNullOrEmpty(ghnTokenDev) || string.IsNullOrEmpty(ghnDevBaseUrl))
    throw new InvalidOperationException("Lỗi cấu hình: GhnSettings:TokenDev hoặc DevBaseAddress chưa được đặt.");

builder.Services.AddAuthorization();

builder.Services.AddHttpClient("GhnProdClient", client =>
{
    client.BaseAddress = new Uri(ghnProdBaseUrl);
    client.DefaultRequestHeaders.Add("Token", ghnToken);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient("GhnDevClient", client =>
{
    client.BaseAddress = new Uri(ghnDevBaseUrl);
    client.DefaultRequestHeaders.Add("Token", ghnTokenDev);
    client.DefaultRequestHeaders.Add("ShopId", ghnShopId);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// ================== CORS (cho Next.js FE) ==================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        // THÊM CÁC DOMAIN CỦA VERCEL VÀO ĐÂY
        policy.WithOrigins(
            "http://localhost:3000",
            "https://cb-gift-fe-sby6-mazut4syf-bachquangles-projects.vercel.app", // Domain Preview 
            "https://cb-gift-fe-sby6.vercel.app" // Domain Production 
        )
             .AllowAnyHeader()
             .AllowAnyMethod()
             .AllowCredentials(); // SignalR
    });
});
// Luôn đăng ký Service thật vào container, vì thằng Demo cần gọi thằng này
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
    builder.Services.AddScoped<IShippingService>(provider =>
        provider.GetRequiredService<GhnShippingService>());
}

// ================== 6. CORS (Update thêm port Vite cho chắc) ==================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Bắt buộc để Cookie hoạt động
    });
});
// Đăng ký CloudinarySettings
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));

// ================== 7. Register Services (DI) ==================
// QUAN TRỌNG: Đăng ký JwtTokenService vào ITokenService
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IManagementAccountService, ManagementAccountService>();

// Các service nghiệp vụ khác
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
builder.Services.AddScoped<IOrderService, OrderService>();
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

    // Trigger hằng ngày 00:05
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("groupOrdersTrigger")
        .StartNow()
        .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(0, 5))
    );
    var invoiceJobKey = new JobKey("createMonthlyInvoicesJob");
    q.AddJob<CreateMonthlyInvoicesJob>(opts => opts.WithIdentity(invoiceJobKey));
    // 2. Trigger chạy vào ngày 10 hàng tháng, lúc 00:05
    q.AddTrigger(opts => opts
        .ForJob(invoiceJobKey)
        .WithIdentity("monthlyInvoiceTrigger")
        .StartNow()
        // Sử dụng builder để đặt lịch: Ngày 10, Giờ 0 (12 AM), Phút 5
        .WithSchedule(CronScheduleBuilder.MonthlyOnDayAndHourAndMinute(10, 0, 5))
    // Hoặc Cron Expression: "0 5 0 10 * ?"
    );
});
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

var app = builder.Build();

// ================== Middleware Pipeline ==================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 1. CORS (Phải trên cùng)
app.UseCors("AllowFrontend");

// 2. Auth (Xác thực User từ Cookie/Token)
app.UseAuthentication();

// 3. Authorization (Phân quyền)
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/notificationHub");

// ================== Seed Data (Thêm Try-Catch để an toàn) ==================
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
        // Log lỗi nếu Seed data chết để không làm sập app thầm lặng
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi xảy ra khi Seeding dữ liệu.");
    }
}

app.Run();

// ================== Seed Functions ==================
// ... (Phần Seed Functions giữ nguyên như của bạn) ...
static async Task SeedRolesAsync(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = { "Admin", "Seller", "Designer", "QC", "Staff", "Manager" };

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

    string[] roleNames = { "Admin", "Seller", "Designer", "QC", "Staff", "Manager" };

    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    var usersToSeed = new List<(string Email, string Password, string Role)>
    {
        ("admin@example.com", "Admin@123", "Admin"),
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