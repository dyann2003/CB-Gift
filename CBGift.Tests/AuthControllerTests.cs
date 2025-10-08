using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using CB_Gift.Controllers;
using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Services.Email;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Abstractions;
using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;

public class AuthControllerTests
{
    private readonly ITestOutputHelper _out;
    private static readonly JsonSerializerOptions JsonOpt = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AuthControllerTests(ITestOutputHelper output) => _out = output;

    private IConfiguration BuildConfig() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:ExpiresMinutes"] = "60",
            ["App:ClientUrl"] = "https://example.com"
        }).Build();

    private static Mock<UserManager<AppUser>> CreateUserManagerWithOptions()
    {
        var store = new Mock<IUserStore<AppUser>>();
        var options = Options.Create(new IdentityOptions());
        var pwdHasher = new Mock<IPasswordHasher<AppUser>>().Object;
        var userValidators = new List<IUserValidator<AppUser>>();
        var pwdValidators = new List<IPasswordValidator<AppUser>>();
        var normalizer = new Mock<ILookupNormalizer>().Object;
        var errors = new IdentityErrorDescriber();
        var services = new Mock<IServiceProvider>().Object;
        var logger = new Mock<ILogger<UserManager<AppUser>>>().Object;

        return new Mock<UserManager<AppUser>>(
            store.Object, options, pwdHasher, userValidators, pwdValidators,
            normalizer, errors, services, logger);
    }

    private static Mock<SignInManager<AppUser>> CreateSignInManagerMock(UserManager<AppUser> um, HttpContext? ctx = null)
    {
        return new Mock<SignInManager<AppUser>>(
            um,
            new HttpContextAccessor { HttpContext = ctx ?? new DefaultHttpContext() },
            Mock.Of<IUserClaimsPrincipalFactory<AppUser>>(),
            null, null, null, null)
        { CallBase = true };
    }

    private void LogResult(string label, IActionResult result)
    {
        object? body = result switch
        {
            OkObjectResult ok => ok.Value,
            BadRequestObjectResult bad => bad.Value,
            UnauthorizedObjectResult un => un.Value,
            _ => null
        };
        var json = body is null ? "" : JsonSerializer.Serialize(body, JsonOpt);
        _out.WriteLine($"[{label}] {result.GetType().Name} :: {json}");
    }

    // ===== Login
    [Fact]
    public async Task Login_Returns_Unauthorized_When_User_NotFound_Or_Inactive()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var tokens = new Mock<ITokenService>();
        var accounts = new Mock<IAccountService>();
        var config = BuildConfig();

        um.Setup(m => m.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((AppUser)null!);
        um.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((AppUser)null!);

        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r1 = await ctl.Login(new LoginDto { UserNameOrEmail = "nope", Password = "x" });
        LogResult("Login_NotFound", r1);
        r1.Should().BeOfType<UnauthorizedObjectResult>();

        var inactive = new AppUser { UserName = "u", Email = "u@x.com", IsActive = false };
        um.Setup(m => m.FindByNameAsync("u")).ReturnsAsync(inactive);
        var r2 = await ctl.Login(new LoginDto { UserNameOrEmail = "u", Password = "x" });
        LogResult("Login_Inactive", r2);
        r2.Should().BeOfType<UnauthorizedObjectResult>();
    }
    [Fact]
    public async Task Login_Returns_Unauthorized_When_Bad_Password()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var tokens = new Mock<ITokenService>();
        var accounts = new Mock<IAccountService>();
        var config = BuildConfig();
        var user = new AppUser { UserName = "u", Email = "u@x.com", IsActive = true };
        um.Setup(m => m.FindByNameAsync("u")).ReturnsAsync(user);
        um.Setup(m => m.FindByEmailAsync("u")).ReturnsAsync((AppUser)null!);

        sm.Setup(s => s.CheckPasswordSignInAsync(user, "bad", false))
          .ReturnsAsync(IdentitySignInResult.Failed);

        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.Login(new LoginDto { UserNameOrEmail = "u", Password = "bad" });
        LogResult("Login_BadPassword", r);
        r.Should().BeOfType<UnauthorizedObjectResult>();
    }
    [Fact]
    public async Task Login_Sets_Cookie_And_Returns_Token_On_Success()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var tokens = new Mock<ITokenService>();
        var accounts = new Mock<IAccountService>();
        var config = BuildConfig();

        var user = new AppUser { UserName = "okuser", Email = "ok@x.com", IsActive = true };
        um.Setup(m => m.FindByNameAsync("okuser")).ReturnsAsync(user);
        um.Setup(m => m.FindByEmailAsync("okuser")).ReturnsAsync((AppUser)null!);

        sm.Setup(s => s.CheckPasswordSignInAsync(user, "good", false)).ReturnsAsync(IdentitySignInResult.Success);
        tokens.Setup(t => t.CreateTokenAsync(user)).ReturnsAsync("jwt-token");

        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var result = await ctl.Login(new LoginDto { UserNameOrEmail = "okuser", Password = "good" });
        LogResult("Login_Success", result);
        result.Should().BeOfType<OkObjectResult>();

        http.Response.Headers.TryGetValue("Set-Cookie", out var setCookies).Should().BeTrue();
        setCookies.ToString().Should().Contain("access_token=");
    }
    [Fact]
    public async Task Login_ByEmail_Sets_Cookie_And_Returns_Token()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var tokens = new Mock<ITokenService>();
        var accounts = new Mock<IAccountService>();
        var config = BuildConfig();

        um.Setup(m => m.FindByNameAsync("mailuser")).ReturnsAsync((AppUser)null!);
        var user = new AppUser { UserName = "mailuser", Email = "mailuser@x.com", IsActive = true };
        um.Setup(m => m.FindByEmailAsync("mailuser")).ReturnsAsync(user);

        sm.Setup(s => s.CheckPasswordSignInAsync(user, "good", false)).ReturnsAsync(IdentitySignInResult.Success);
        tokens.Setup(t => t.CreateTokenAsync(user)).ReturnsAsync("jwt-token");

        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.Login(new LoginDto { UserNameOrEmail = "mailuser", Password = "good" });
        LogResult("Login_ByEmail", r);
        r.Should().BeOfType<OkObjectResult>();
    }
    [Fact]
    public async Task Login_Returns_Unauthorized_When_LockedOut()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var tokens = new Mock<ITokenService>();
        var accounts = new Mock<IAccountService>();
        var config = BuildConfig();

        var user = new AppUser { UserName = "lock", Email = "lock@x.com", IsActive = true };
        um.Setup(m => m.FindByNameAsync("lock")).ReturnsAsync(user);
        um.Setup(m => m.FindByEmailAsync("lock")).ReturnsAsync((AppUser)null!);
        sm.Setup(s => s.CheckPasswordSignInAsync(user, "pwd", false)).ReturnsAsync(IdentitySignInResult.LockedOut);

        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.Login(new LoginDto { UserNameOrEmail = "lock", Password = "pwd" });
        LogResult("Login_LockedOut", r);
        r.Should().BeOfType<UnauthorizedObjectResult>();
    }


    [Fact]
    public async Task Login_Empty_Username_Or_Password_ModelInvalid_Returns_BadRequest()
    {
        var http = new DefaultHttpContext();
        var ctl = new AuthController(
            CreateSignInManagerMock(CreateUserManagerWithOptions().Object, http).Object,
            CreateUserManagerWithOptions().Object,
            Mock.Of<ITokenService>(),
            BuildConfig(),
            Mock.Of<IAccountService>())
        { ControllerContext = new ControllerContext { HttpContext = http } };

        ctl.ModelState.AddModelError("UserNameOrEmail", "Required");
        var r1 = await ctl.Login(new LoginDto { UserNameOrEmail = "", Password = "x" });
        LogResult("Login_EmptyUsername", r1);
        r1.Should().BeOfType<BadRequestObjectResult>(); 
        ctl.ModelState.Clear();
        ctl.ModelState.AddModelError("Password", "Required");
        var r2 = await ctl.Login(new LoginDto { UserNameOrEmail = "u", Password = "" });
        LogResult("Login_EmptyUsername", r2);
        r2.Should().BeOfType<BadRequestObjectResult>();
    }
    

    [Fact]
    public async Task Login_UserName_CaseInsensitive_Succeeds()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var tokens = new Mock<ITokenService>();
        var config = BuildConfig();

        var user = new AppUser { UserName = "okuser", Email = "ok@x.com", IsActive = true };

        // Giả lập hệ thống không phân biệt hoa/thường → trả user khi nhập "OKUSER"
        um.Setup(m => m.FindByNameAsync("OKUSER")).ReturnsAsync(user);

        sm.Setup(s => s.CheckPasswordSignInAsync(user, "Password123@", false)).ReturnsAsync(IdentitySignInResult.Success);
        tokens.Setup(t => t.CreateTokenAsync(user)).ReturnsAsync("jwt");

        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, Mock.Of<IAccountService>())
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.Login(new LoginDto { UserNameOrEmail = "OKUSER", Password = "Password123@" });
        LogResult("Login_Username_CaseInsensitive", r);
        r.Should().BeOfType<OkObjectResult>();
    }
    [Fact]
    public async Task Login_Email_CaseInsensitive_Succeeds()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var tokens = new Mock<ITokenService>();
        var config = BuildConfig();

        var user = new AppUser { UserName = "mailuser", Email = "mailuser@x.com", IsActive = true };

        // username không có → fallback sang email; nhập HOA
        um.Setup(m => m.FindByNameAsync("MAILUSER@X.COM")).ReturnsAsync((AppUser)null!);
        um.Setup(m => m.FindByEmailAsync("MAILUSER@X.COM")).ReturnsAsync(user);

        sm.Setup(s => s.CheckPasswordSignInAsync(user, "good", false)).ReturnsAsync(IdentitySignInResult.Success);
        tokens.Setup(t => t.CreateTokenAsync(user)).ReturnsAsync("jwt");

        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, Mock.Of<IAccountService>())
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.Login(new LoginDto { UserNameOrEmail = "MAILUSER@X.COM", Password = "good" });
        LogResult("Login_Email_CaseInsensitive", r);
        r.Should().BeOfType<OkObjectResult>();
    }


    // ===== Logout
    [Fact]
    public void Logout_Deletes_Cookie_And_Returns_Ok()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var tokens = new Mock<ITokenService>();
        var accounts = new Mock<IAccountService>();
        var config = BuildConfig();

        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var result = ctl.Logout();
        LogResult("Logout", result);
        result.Should().BeOfType<OkObjectResult>();
    }
    //new
    [Fact]
    public void Logout_Deletes_Cookie_Header_Has_Delete_Semantics()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), BuildConfig(), Mock.Of<IAccountService>())
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = ctl.Logout();
        r.Should().BeOfType<OkObjectResult>();

        http.Response.Headers.TryGetValue("Set-Cookie", out var setCookies).Should().BeTrue();
        var cookie = setCookies.ToString();
        cookie.Should().Contain("access_token=");
        cookie.Should().Contain("expires="); // ASP.NET Delete đặt expires quá khứ
    }


    // ===== Change password
    [Fact]
    public async Task ChangePassword_Returns_Unauthorized_When_User_Not_In_Context()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), BuildConfig(), Mock.Of<IAccountService>())
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ChangePassword(new ChangePasswordDto { CurrentPassword = "oldpass123@", NewPassword = "newpass123@" });
        LogResult("ChangePassword_Unauthorized", r);
        r.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task ChangePassword_Returns_BadRequest_When_Manager_Fails()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);

        var user = new AppUser { Id = "U100", UserName = "test", Email = "t@x.com" };
        um.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        um.Setup(m => m.ChangePasswordAsync(user, "oldpass123@", "newpass123@"))
          .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "E1", Description = "boom" }));

        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), BuildConfig(), Mock.Of<IAccountService>())
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ChangePassword(new ChangePasswordDto { CurrentPassword = "oldpass123@", NewPassword = "newpass123@" });
        LogResult("ChangePassword_BadRequest", r); // serialize body -> force LINQ
        r.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ChangePassword_Returns_Ok_On_Success()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);

        var user = new AppUser { Id = "U101", UserName = "ok", Email = "ok@x.com" };
        um.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        um.Setup(m => m.ChangePasswordAsync(user, "old", "new")).ReturnsAsync(IdentityResult.Success);

        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), BuildConfig(), Mock.Of<IAccountService>())
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ChangePassword(new ChangePasswordDto { CurrentPassword = "old", NewPassword = "new" });
        LogResult("ChangePassword_Success", r);
        r.Should().BeOfType<OkObjectResult>();
    }
    [Fact]
    public async Task ChangePassword_Returns_BadRequest_When_CurrentPassword_Wrong()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);

        var user = new AppUser { Id = "U200", UserName = "u", Email = "u@x.com" };
        um.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        um.Setup(m => m.ChangePasswordAsync(user, "Passwordwrong", "Newpass@123"))
          .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordMismatch", Description = "Incorrect password." }));

        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), BuildConfig(), Mock.Of<IAccountService>())
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ChangePassword(new ChangePasswordDto { CurrentPassword = "Passwordwrong", NewPassword = "Newpass@123" });
        r.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ChangePassword_Returns_BadRequest_When_NewPassword_TooWeak()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);

        var user = new AppUser { Id = "U201", UserName = "weak", Email = "w@x.com" };
        um.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        um.Setup(m => m.ChangePasswordAsync(user, "Old@123", "short"))
          .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordTooShort", Description = "Too short." }));

        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), BuildConfig(), Mock.Of<IAccountService>())
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ChangePassword(new ChangePasswordDto { CurrentPassword = "Old@123", NewPassword = "short" });
        r.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ChangePassword_Returns_BadRequest_When_NewPassword_FailsPolicy()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);

        var user = new AppUser { Id = "U201", UserName = "u2", Email = "u2@x.com" };
        um.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        um.Setup(m => m.ChangePasswordAsync(user, "PasswordOld@123", "short"))
          .ReturnsAsync(IdentityResult.Failed(
              new IdentityError { Code = "PasswordTooShort", Description = "Too short." },
              new IdentityError { Code = "PasswordRequiresDigit", Description = "Need a digit." }
          ));

        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), BuildConfig(), Mock.Of<IAccountService>())
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ChangePassword(new ChangePasswordDto { CurrentPassword = "PasswordOld@123", NewPassword = "short" });
        r.Should().BeOfType<BadRequestObjectResult>();
    }
    [Fact]
    public async Task ChangePassword_NewPassword_NullOrEmpty_ModelInvalid_Returns_BadRequest()
    {
        var http = new DefaultHttpContext();
        var ctl = new AuthController(
            CreateSignInManagerMock(CreateUserManagerWithOptions().Object, http).Object,
            CreateUserManagerWithOptions().Object,
            Mock.Of<ITokenService>(),
            BuildConfig(),
            Mock.Of<IAccountService>())
        { ControllerContext = new ControllerContext { HttpContext = http } };

        ctl.ModelState.AddModelError("NewPassword", "Required");
        var r1 = await ctl.ChangePassword(new ChangePasswordDto { CurrentPassword = "Old@123", NewPassword = null! });
        LogResult("ChangePassword_NewPassword_Null", r1);
        r1.Should().BeOfType<BadRequestObjectResult>();

        ctl.ModelState.Clear();
        ctl.ModelState.AddModelError("NewPassword", "Required");
        var r2 = await ctl.ChangePassword(new ChangePasswordDto { CurrentPassword = "Old@123", NewPassword = "" });
        LogResult("ChangePassword_NewPassword_Empty", r2);
        r2.Should().BeOfType<BadRequestObjectResult>();
    }


    // ===== Register
    [Fact]
    public async Task Register_Returns_BadRequest_When_AccountService_Fails()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var accounts = new Mock<IAccountService>();
        accounts.Setup(a => a.RegisterAsync(It.IsAny<RegisterRequestDto>()))
                .ReturnsAsync(new ServiceResult<RegisterResponseDto> { Success = false, Message = "Email already registered." });

        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), BuildConfig(), accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.Register(new RegisterRequestDto { Email = "dup@x.com" });
        LogResult("Register_Failed", r);
        r.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Register_Returns_Ok_With_Data_On_Success()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var accounts = new Mock<IAccountService>();
        var data = new RegisterResponseDto { Email = "new@x.com", TemporaryPassword = "abc123" };
        accounts.Setup(a => a.RegisterAsync(It.IsAny<RegisterRequestDto>()))
                .ReturnsAsync(new ServiceResult<RegisterResponseDto> { Success = true, Data = data });

        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), BuildConfig(), accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.Register(new RegisterRequestDto { Email = "new@x.com" });
        LogResult("Register_Success", r);
        r.Should().BeOfType<OkObjectResult>();
    }

    // ===== Forgot password
    [Fact]
    public async Task ForgotPassword_Returns_BadRequest_When_User_NotFound()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);

        um.Setup(m => m.FindByEmailAsync("notfound@example.com")).ReturnsAsync((AppUser)null!);

        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), BuildConfig(), Mock.Of<IAccountService>())
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ForgotPassword(new ForgotPasswordDto { Email = "notfound@example.com" });
        LogResult("Forgot_UserNotFound", r);
        r.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ForgotPassword_Returns_BadRequest_When_Email_Not_Confirmed()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);

        var user = new AppUser { Email = "notconfirm@gmail.com" };
        um.Setup(m => m.FindByEmailAsync("notconfirm@gmail.com")).ReturnsAsync(user);
        um.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(false);

        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), BuildConfig(), Mock.Of<IAccountService>())
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ForgotPassword(new ForgotPasswordDto { Email = "notconfirm@gmail.com" });
        LogResult("Forgot_NotConfirmed", r);
        r.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ForgotPassword_Sends_Reset_Email_And_Returns_Ok()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var accounts = new Mock<IAccountService>();
        var config = BuildConfig();

        var user = new AppUser { Email = "test@example.com" };
        um.Setup(m => m.FindByEmailAsync("test@example.com")).ReturnsAsync(user);
        um.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
        um.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("tok123");

        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), config, accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ForgotPassword(new ForgotPasswordDto { Email = "test@example.com" });
        LogResult("Forgot_Success", r);
        r.Should().BeOfType<OkObjectResult>();

        accounts.Verify(a => a.SendResetPasswordEmailAsync(
            "test@example.com",
            It.Is<string>(link => link.Contains("reset-password") && link.Contains("tok123"))),
            Times.Once);
    }
    [Fact]
    public async Task ForgotPassword_Returns_BadRequest_When_Email_Null_ModelInvalid()
    {
        var http = new DefaultHttpContext();
        var ctl = new AuthController(
            CreateSignInManagerMock(CreateUserManagerWithOptions().Object, http).Object,
            CreateUserManagerWithOptions().Object,
            Mock.Of<ITokenService>(),
            BuildConfig(),
            Mock.Of<IAccountService>())
        { ControllerContext = new ControllerContext { HttpContext = http } };

        // Giả lập [ApiController] model validation
        ctl.ModelState.AddModelError("Email", "The Email field is required.");

        var r = await ctl.ForgotPassword(new ForgotPasswordDto { Email = null! });
        r.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ForgotPassword_Returns_BadRequest_When_Email_InvalidFormat_ModelInvalid()
    {
        var http = new DefaultHttpContext();
        var ctl = new AuthController(
            CreateSignInManagerMock(CreateUserManagerWithOptions().Object, http).Object,
            CreateUserManagerWithOptions().Object,
            Mock.Of<ITokenService>(),
            BuildConfig(),
            Mock.Of<IAccountService>())
        { ControllerContext = new ControllerContext { HttpContext = http } };

        // Nếu ForgotPasswordDto có [EmailAddress], thêm lỗi format
        ctl.ModelState.AddModelError("Email", "The Email field is not a valid e-mail address.");

        var r = await ctl.ForgotPassword(new ForgotPasswordDto { Email = "not-an-email" });
        r.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ForgotPassword_Returns_Unauthorized_When_User_Inactive()
    {
        // Arrange
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var accounts = new Mock<IAccountService>();
        var config = BuildConfig();

        // User tồn tại nhưng chưa active
        var inactiveUser = new AppUser
        {
            Email = "inactive@gmail.com",
            IsActive = false,
            UserName = "inactive_user"
        };

        // Mock hành vi UserManager
        um.Setup(m => m.FindByEmailAsync("inactive@gmail.com")).ReturnsAsync(inactiveUser);
        um.Setup(m => m.IsEmailConfirmedAsync(inactiveUser)).ReturnsAsync(true);

        // Act
        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), config, accounts.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };

        var r = await ctl.ForgotPassword(new ForgotPasswordDto { Email = "inactive@gmail.com" });
        LogResult("ForgotPassword_UserInactive", r);

        // Assert
        r.Should().BeOfType<UnauthorizedObjectResult>();
        accounts.Verify(a => a.SendResetPasswordEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }


    // ===== Reset password
    [Fact]
    public async Task ResetPassword_Returns_BadRequest_When_User_NotFound()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);

        um.Setup(m => m.FindByEmailAsync("none@gmail.com")).ReturnsAsync((AppUser)null!);

        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), BuildConfig(), Mock.Of<IAccountService>())
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ResetPassword(new ResetPasswordDto { Email = "none@gmail.com", Token = "token", NewPassword = "Password@ss1" });
        LogResult("Reset_UserNotFound", r);
        r.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ResetPassword_Returns_BadRequest_When_Reset_Fails()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);

        var user = new AppUser { Email = "user123@gmail.com" };
        um.Setup(m => m.FindByEmailAsync("user123@gmail.com")).ReturnsAsync(user);
        um.Setup(m => m.ResetPasswordAsync(user, "token", "NewPass@ss1"))
          .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "invalid token" }));

        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), BuildConfig(), Mock.Of<IAccountService>())
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ResetPassword(new ResetPasswordDto { Email = "user123@gmail.com", Token = "token", NewPassword = "NewPass@ss1" });
        LogResult("Reset_Failed", r); // serialize -> evaluate LINQ
        r.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ResetPassword_Returns_Ok_On_Success()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);

        var user = new AppUser { Email = "okuser@gmail.com" };
        um.Setup(m => m.FindByEmailAsync("okuser@gmail.com")).ReturnsAsync(user);
        um.Setup(m => m.ResetPasswordAsync(user, "token", "NewPass@ss1")).ReturnsAsync(IdentityResult.Success);

        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), BuildConfig(), Mock.Of<IAccountService>())
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ResetPassword(new ResetPasswordDto { Email = "okuser@gmail.com", Token = "token", NewPassword = "NewPass@ss1" });
        LogResult("Reset_Success", r);
        r.Should().BeOfType<OkObjectResult>();
    }
 

    [Fact]
    public async Task ResetPassword_Returns_BadRequest_When_Email_Null_Or_Empty()
    {
        var http = new DefaultHttpContext();
        var ctl = new AuthController(
            CreateSignInManagerMock(CreateUserManagerWithOptions().Object, http).Object,
            CreateUserManagerWithOptions().Object,
            Mock.Of<ITokenService>(),
            BuildConfig(),
            Mock.Of<IAccountService>())
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r1 = await ctl.ResetPassword(new ResetPasswordDto { Email = null!, Token = "token", NewPassword = "NewPass@ss1" });
        LogResult("Reset email can not null", r1);
        r1.Should().BeOfType<BadRequestObjectResult>();

        var r2 = await ctl.ResetPassword(new ResetPasswordDto { Email = "", Token = "token", NewPassword = "NewPass@ss1" });
        LogResult("Reset email can not empty", r1);
        r2.Should().BeOfType<BadRequestObjectResult>();
    }
    [Fact]
    public async Task ResetPassword_Returns_BadRequest_When_Token_Null_ModelInvalid()
    {
        var http = new DefaultHttpContext();
        var ctl = new AuthController(
            CreateSignInManagerMock(CreateUserManagerWithOptions().Object, http).Object,
            CreateUserManagerWithOptions().Object,
            Mock.Of<ITokenService>(),
            BuildConfig(),
            Mock.Of<IAccountService>())
        { ControllerContext = new ControllerContext { HttpContext = http } };

        ctl.ModelState.AddModelError("Token", "The Token field is required.");

        var r = await ctl.ResetPassword(new ResetPasswordDto { Email = "usernulltoken@gmail.com", Token = null!, NewPassword = "NewPass@ss1" });
        r.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ResetPassword_Returns_BadRequest_When_NewPassword_FailsPolicy()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);

        var user = new AppUser { Email = "user123@gmail.com" };
        um.Setup(m => m.FindByEmailAsync("user123@gmail.com")).ReturnsAsync(user);

        // Giả lập lỗi policy (quá ngắn/thiếu ký tự đặc biệt, v.v.)
        um.Setup(m => m.ResetPasswordAsync(user, "token", "short"))
          .ReturnsAsync(IdentityResult.Failed(
              new IdentityError { Code = "PasswordTooShort", Description = "Too short." },
              new IdentityError { Code = "PasswordRequiresNonAlphanumeric", Description = "Need special char." }
          ));

        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), BuildConfig(), Mock.Of<IAccountService>())
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ResetPassword(new ResetPasswordDto { Email = "user123@gmail.com", Token = "token", NewPassword = "short" });
        LogResult("Reset_NewPassword_FailsPolicy", r);
        r.Should().BeOfType<BadRequestObjectResult>();
    }
    [Fact]
    public async Task ResetPassword_NewPassword_NullOrEmpty_ModelInvalid_Returns_BadRequest()
    {
        var http = new DefaultHttpContext();
        var ctl = new AuthController(
            CreateSignInManagerMock(CreateUserManagerWithOptions().Object, http).Object,
            CreateUserManagerWithOptions().Object,
            Mock.Of<ITokenService>(),
            BuildConfig(),
            Mock.Of<IAccountService>())
        { ControllerContext = new ControllerContext { HttpContext = http } };

        // Null
        ctl.ModelState.AddModelError("NewPassword", "Required");
        var r1 = await ctl.ResetPassword(new ResetPasswordDto { Email = "user123@gmail.com", Token = "token", NewPassword = null! });
        LogResult("Reset_NewPassword_Null", r1);
        r1.Should().BeOfType<BadRequestObjectResult>();

        // Empty
        ctl.ModelState.Clear();
        ctl.ModelState.AddModelError("NewPassword", "Required");
        var r2 = await ctl.ResetPassword(new ResetPasswordDto { Email = "user123@gmail.com", Token = "token", NewPassword = "" });
        LogResult("Reset_NewPassword_Empty", r2);
        r2.Should().BeOfType<BadRequestObjectResult>();
    }


}

