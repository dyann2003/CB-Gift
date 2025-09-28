using System.Text;
using CB_Gift.Data;
using CB_Gift.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger
builder.Services.AddControllers();
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

// DbContext
builder.Services.AddDbContext<CBGiftDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
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

// JWT
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

// Token service
builder.Services.AddScoped<ITokenService, JwtTokenService>();

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 👇 Thứ tự đúng cho auth
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ===== Seed 1 tài khoản demo để login =====
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

    var seedEmail = builder.Configuration["Seed:Email"] ?? "demo@cbgift.local";
    var seedPass = builder.Configuration["Seed:Password"] ?? "Demo@123!";

    var user = await userManager.FindByEmailAsync(seedEmail);
    if (user is null)
    {
        user = new AppUser
        {
            UserName = seedEmail,
            Email = seedEmail,
            EmailConfirmed = true,
            FullName = "Demo User",
            IsActive = true
        };
        var create = await userManager.CreateAsync(user, seedPass);
        if (!create.Succeeded)
        {
            Console.WriteLine("Seed user failed: " +
                string.Join("; ", create.Errors.Select(e => $"{e.Code}:{e.Description}")));
        }
        else
        {
            Console.WriteLine($"Seed user created: {seedEmail} / {seedPass}");
        }
    }
    else
    {
        Console.WriteLine($"Seed user exists: {seedEmail}");
    }
}

app.Run();
