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

// ================== Controllers + Swagger ==================
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
        Description = "JWT Authorization header. Nhập token vào đây (không cần gõ 'Bearer ')",
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

// ================== DbContext ==================
// Lấy chuỗi kết nối từ appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

//Dùng UseMySql
builder.Services.AddDbContext<CBGiftDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

// ================== Identity ==================
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

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// ================== JWT ==================
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
            ClockSkew = TimeSpan.Zero
        };

        // đọc JWT từ cookie "access_token"
        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                // Lấy token từ query cho SignalR
                var accessToken = ctx.Request.Query["access_token"];

                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/notificationHub"))
                {
                    ctx.Token = accessToken;
                }

                // Hoặc lấy từ cookie
                if (ctx.Token == null &&
                    ctx.Request.Cookies.TryGetValue("access_token", out var cookieToken))
                {
                    ctx.Token = cookieToken;
                }

                return Task.CompletedTask;
            }
        };
    });

// ========= Cấu hình GHN =========
// Lấy cấu hình GhnSettings
var ghnSettings = builder.Configuration.GetSection("GhnSettings");

// Lấy Token và BaseAddress
var ghnToken = ghnSettings["Token"];
var ghnProdBaseUrl = ghnSettings["ProdBaseAddress"];

var ghnTokenDev = ghnSettings["TokenDev"];
var ghnDevBaseUrl = ghnSettings["DevBaseAddress"];
var ghnShopId = ghnSettings["ShopId"];

// Kiểm tra null
if (string.IsNullOrEmpty(ghnToken) || string.IsNullOrEmpty(ghnProdBaseUrl))
{
    throw new InvalidOperationException("Lỗi cấu hình: GhnSettings:Token hoặc ProdBaseAddress chưa được đặt.");
}
if (string.IsNullOrEmpty(ghnTokenDev) || string.IsNullOrEmpty(ghnDevBaseUrl))
{
    throw new InvalidOperationException("Lỗi cấu hình: GhnSettings:TokenDev hoặc DevBaseAddress chưa được đặt.");
}
// -------------------------


builder.Services.AddAuthorization();

// 1. Client cho môi trường PROD (Lấy Tỉnh/Huyện/Xã)
builder.Services.AddHttpClient("GhnProdClient", client =>
{
    client.BaseAddress = new Uri(ghnProdBaseUrl);
    client.DefaultRequestHeaders.Add("Token", ghnToken);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// 2. Client cho môi trường DEV (Tạo đơn, Leadtime...)
builder.Services.AddHttpClient("GhnDevClient", client =>
{
    client.BaseAddress = new Uri(ghnDevBaseUrl);
    client.DefaultRequestHeaders.Add("Token", ghnTokenDev);
    client.DefaultRequestHeaders.Add("ShopId", ghnShopId);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// ------------------------------------

// ================== CORS (cho Next.js FE) ==================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        // THÊM CÁC DOMAIN CỦA VERCEL VÀO ĐÂY
        policy.WithOrigins(
            "http://localhost:3000",
            "https://cb-gift-fe-sby6-mazut4syf-bachquangles-projects.vercel.app", // Domain Preview 
            "https://cb-gift-fe-sby6.vercel.app", // Domain Production 
            "https://*.vercel.app" // TÙY CHỌN: Cho phép tất cả các bản Preview trên Vercel
        )
             .AllowAnyHeader()
             .AllowAnyMethod()
             .AllowCredentials(); // SignalR
    });
});

// Đăng ký CloudinarySettings
builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("CloudinarySettings"));

// ================== Custom services ==================
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
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
builder.Services.AddScoped<IShippingService, GhnShippingService>();
builder.Services.AddScoped<IGhnPrintService, GhnPrintService>();
builder.Services.AddScoped<IManagementAccountService, ManagementAccountService>();
builder.Services.AddSingleton<NotificationHub>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<OrderFactory>();
builder.Services.AddScoped<ReferenceDataCache>();
builder.Services.AddScoped<IValidator<OrderImportRowDto>, OrderImportRowValidator>();
builder.Services.AddScoped<IReportService, ReportService>();

// --- Quartz ---
builder.Services.AddQuartz(q =>
{
    // Job factory được DI tự động
    q.SchedulerId = "Scheduler-Core";

    // Đăng ký Job
    var jobKey = new JobKey("groupOrdersJob");
    q.AddJob<GroupOrdersJob>(opts => opts.WithIdentity(jobKey));

    // Trigger hằng ngày 00:05
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("groupOrdersTrigger")
        .StartNow()
        .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(0, 5)) // chạy 00:05 hàng ngày
    );
});

// Quartz hosted service chạy cùng ứng dụng
builder.Services.AddQuartzHostedService(options =>
{
    options.WaitForJobsToComplete = true;
});

var app = builder.Build();

// ================== Middleware ==================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// bật CORS trước Authentication
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map Hub tới một endpoint
// Client sẽ kết nối tới URL "/notificationHub"
app.MapHub<CB_Gift.Hubs.NotificationHub>("/notificationHub");

// ================== Seed Roles + User ==================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedRolesAsync(services);
    await SeedAllDataAsync(services);
    var cache = scope.ServiceProvider.GetRequiredService<ReferenceDataCache>();
    await cache.LoadAsync();
}

app.Run();

// ================== Seed functions ==================
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
    // 1. Lấy các service cần thiết
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

    // 2. Khởi tạo danh sách các Roles cần có
    string[] roleNames = { "Admin", "Seller", "Designer", "QC", "Staff", "Manager" };

    foreach (var roleName in roleNames)
    {
        var roleExist = await roleManager.RoleExistsAsync(roleName);
        if (!roleExist)
        {
            // Tạo role mới nếu chưa tồn tại
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // 3. Khởi tạo danh sách Users cần seed (Email, Mật khẩu, Role)
    var usersToSeed = new List<(string Email, string Password, string Role)>
    {
        ("admin@example.com", "Admin@123", "Admin"),
        ("seller@example.com", "Seller@123", "Seller"),
        ("designer@example.com", "Designer@123", "Designer"),
        ("qc@example.com", "Qc@123", "QC"),
        ("staff@example.com", "Staff@123", "Staff"),
        ("manager@example.com", "Manager@123", "Manager")
    };

    // 4. Vòng lặp để tạo User và gán Role tương ứng
    foreach (var userSpec in usersToSeed)
    {
        // Kiểm tra xem user đã tồn tại chưa
        var user = await userManager.FindByEmailAsync(userSpec.Email);
        if (user == null)
        {
            var newUser = new AppUser
            {
                UserName = userSpec.Email,
                Email = userSpec.Email,
                EmailConfirmed = true
            };

            // Tạo user mới với mật khẩu đã định
            var result = await userManager.CreateAsync(newUser, userSpec.Password);

            if (result.Succeeded)
            {
                // Gán role cho user vừa tạo
                await userManager.AddToRoleAsync(newUser, userSpec.Role);
            }
        }
    }
}

