﻿using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Jobs;
using CB_Gift.Services;
using CB_Gift.Services.Email;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Quartz;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ================== Controllers + Swagger ==================
builder.Services.AddControllers()
    .AddJsonOptions(x =>
        x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

builder.Services.AddEndpointsApiExplorer();
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
                if (ctx.Request.Cookies.TryGetValue("access_token", out var token))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ================== CORS (cho Next.js FE) ==================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // FE URL
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Đăng ký CloudinarySettings
builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("CloudinarySettings"));

// ================== Custom services ==================
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<IOrderDetailService, OrderDetailService>();
builder.Services.AddScoped<IPlanService, PlanService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.AddScoped<IImageManagementService, ImageManagementService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ITagService, TagService>();

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

// ================== Seed Roles + User ==================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedRolesAsync(services);
    await SeedAllDataAsync(services);
   
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

