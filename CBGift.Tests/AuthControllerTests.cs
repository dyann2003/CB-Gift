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
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
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

    private static bool ToBool(IActionResult result) =>
        result is OkResult || result is OkObjectResult;

    // ===== Login

    [Fact]
    public async Task Login_Returns_False_When_User_NotFound_Or_Inactive()
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

        // Case 1: user not found
        var r1 = await ctl.Login(new LoginDto { UserNameOrEmail = "emailnotfound@gmail.com", Password = "x" });
        bool result1 = r1 is not UnauthorizedObjectResult;
        result1.Should().BeFalse();

        // Case 2: user inactive
        var inactive = new AppUser { UserName = "u", Email = "userinactive@x.com", IsActive = false };
        um.Setup(m => m.FindByNameAsync("u")).ReturnsAsync(inactive);

        var r2 = await ctl.Login(new LoginDto { UserNameOrEmail = "u", Password = "x" });
        bool result2 = r2 is not UnauthorizedObjectResult;
        result2.Should().BeFalse();
    }

    [Fact]
    public async Task Login_Returns_False_When_Bad_Password()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var tokens = new Mock<ITokenService>();
        var accounts = new Mock<IAccountService>();
        var config = BuildConfig();

        var user = new AppUser { UserName = "u", Email = "useinactive@gmail.com", IsActive = true };
        um.Setup(m => m.FindByNameAsync("u")).ReturnsAsync(user);
        um.Setup(m => m.FindByEmailAsync("u")).ReturnsAsync((AppUser)null!);

        sm.Setup(s => s.CheckPasswordSignInAsync(user, "badpass", false))
          .ReturnsAsync(IdentitySignInResult.Failed);

        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.Login(new LoginDto { UserNameOrEmail = "u", Password = "badpass" });
        LogResult("Login_BadPassword", r);

        bool success = r is OkResult || r is OkObjectResult;
        success.Should().BeFalse();
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

        sm.Setup(s => s.CheckPasswordSignInAsync(user, "good", false))
          .ReturnsAsync(IdentitySignInResult.Success);
        tokens.Setup(t => t.CreateTokenAsync(user)).ReturnsAsync("jwt-token");

        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var result = await ctl.Login(new LoginDto { UserNameOrEmail = "okuser", Password = "good" });
        LogResult("Login_Success", result);

        ToBool(result).Should().BeTrue();

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

        sm.Setup(s => s.CheckPasswordSignInAsync(user, "good", false))
          .ReturnsAsync(IdentitySignInResult.Success);
        tokens.Setup(t => t.CreateTokenAsync(user)).ReturnsAsync("jwt-token");

        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.Login(new LoginDto { UserNameOrEmail = "mailuser", Password = "good" });
        LogResult("Login_ByEmail", r);

        ToBool(r).Should().BeTrue();

        http.Response.Headers.TryGetValue("Set-Cookie", out var setCookies).Should().BeTrue();
        setCookies.ToString().Should().Contain("access_token=");
    }

    [Fact]
    public async Task Login_Returns_False_When_LockedOut()
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

        sm.Setup(s => s.CheckPasswordSignInAsync(user, "pwd", false))
          .ReturnsAsync(IdentitySignInResult.LockedOut);

        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.Login(new LoginDto { UserNameOrEmail = "lock", Password = "pwd" });
        LogResult("Login_LockedOut", r);

        ToBool(r).Should().BeFalse();
        tokens.Verify(t => t.CreateTokenAsync(It.IsAny<AppUser>()), Times.Never);
        http.Response.Headers.TryGetValue("Set-Cookie", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Login_ModelInvalid_Returns_False()
    {
        var http = new DefaultHttpContext();
        var ctl = new AuthController(
            CreateSignInManagerMock(CreateUserManagerWithOptions().Object, http).Object,
            CreateUserManagerWithOptions().Object,
            Mock.Of<ITokenService>(),
            BuildConfig(),
            new Mock<IAccountService>().Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        ctl.ModelState.AddModelError("UserNameOrEmail", "Required");
        var r1 = await ctl.Login(new LoginDto { UserNameOrEmail = "", Password = "x" });
        LogResult("Login_EmptyUsername", r1);
        ToBool(r1).Should().BeFalse();

        ctl.ModelState.Clear();
        ctl.ModelState.AddModelError("Password", "Required");
        var r2 = await ctl.Login(new LoginDto { UserNameOrEmail = "u", Password = "" });
        LogResult("Login_EmptyPassword", r2);
        ToBool(r2).Should().BeFalse();

        http.Response.Headers.TryGetValue("Set-Cookie", out _).Should().BeFalse();
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

        um.Setup(m => m.FindByNameAsync("OKUSER")).ReturnsAsync(user);

        sm.Setup(s => s.CheckPasswordSignInAsync(user, "Password123@", false))
          .ReturnsAsync(IdentitySignInResult.Success);
        tokens.Setup(t => t.CreateTokenAsync(user)).ReturnsAsync("jwt");

        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, new Mock<IAccountService>().Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.Login(new LoginDto { UserNameOrEmail = "OKUSER", Password = "Password123@" });
        LogResult("Login_Username_CaseInsensitive", r);

        ToBool(r).Should().BeTrue();

        http.Response.Headers.TryGetValue("Set-Cookie", out var setCookies).Should().BeTrue();
        setCookies.ToString().Should().Contain("access_token=");
        tokens.Verify(t => t.CreateTokenAsync(user), Times.Once);
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

        um.Setup(m => m.FindByNameAsync("MAILUSER@X.COM")).ReturnsAsync((AppUser)null!);
        um.Setup(m => m.FindByEmailAsync("MAILUSER@X.COM")).ReturnsAsync(user);

        sm.Setup(s => s.CheckPasswordSignInAsync(user, "good", false))
          .ReturnsAsync(IdentitySignInResult.Success);
        tokens.Setup(t => t.CreateTokenAsync(user)).ReturnsAsync("jwt");

        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, new Mock<IAccountService>().Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.Login(new LoginDto { UserNameOrEmail = "MAILUSER@X.COM", Password = "good" });
        LogResult("Login_Email_CaseInsensitive", r);

        ToBool(r).Should().BeTrue();

        http.Response.Headers.TryGetValue("Set-Cookie", out var setCookies).Should().BeTrue();
        setCookies.ToString().Should().Contain("access_token=");
        tokens.Verify(t => t.CreateTokenAsync(user), Times.Once);
    }

    // ===== Logout

    [Fact]
    public void Logout_Returns_True_When_Successful()
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

        ToBool(result).Should().BeTrue();

        http.Response.Headers.TryGetValue("Set-Cookie", out var setCookies).Should().BeTrue();
        setCookies.ToString().Should().Contain("access_token=");
    }

    [Fact]
    public void Logout_Deletes_Cookie_Header_Has_Delete_Semantics()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);

        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), BuildConfig(), new Mock<IAccountService>().Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = ctl.Logout();
        ToBool(r).Should().BeTrue();

        http.Response.Headers.TryGetValue("Set-Cookie", out var setCookies).Should().BeTrue();
        var cookie = setCookies.ToString();

        cookie.Should().Contain("access_token=");
        cookie.ToLowerInvariant().Should().Contain("expires="); // chấp nhận khác biệt chữ hoa/thường
    }

    // ===== Change password

    [Fact]
    public async Task ChangePassword_Returns_False_When_User_Not_In_Context()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);

        var ctl = new AuthController(
            sm.Object,
            um.Object,
            Mock.Of<ITokenService>(),
            BuildConfig(),
            new Mock<IAccountService>().Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ChangePassword(new ChangePasswordDto
        {
            CurrentPassword = "oldpass123@",
            NewPassword = "newpass123@"
        });

        LogResult("ChangePassword_Unauthorized", r);
        ToBool(r).Should().BeFalse();
    }

    [Fact]
    public async Task ChangePassword_Returns_False_When_Manager_Fails()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);

        var user = new AppUser { Id = "U100", UserName = "test", Email = "t@x.com" };

        um.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        um.Setup(m => m.ChangePasswordAsync(user, "oldpass123@", "newpass123@"))
          .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "E1", Description = "boom" }));

        var ctl = new AuthController(
            sm.Object,
            um.Object,
            Mock.Of<ITokenService>(),
            BuildConfig(),
            new Mock<IAccountService>().Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ChangePassword(new ChangePasswordDto
        {
            CurrentPassword = "oldpass123@",
            NewPassword = "newpass123@"
        });

        LogResult("ChangePassword_Failed", r);
        ToBool(r).Should().BeFalse();
    }

    [Fact]
    public async Task ChangePassword_Returns_True_On_Success()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);

        var user = new AppUser { Id = "U101", UserName = "ok", Email = "ok@x.com" };
        um.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        um.Setup(m => m.ChangePasswordAsync(user, "old", "new"))
          .ReturnsAsync(IdentityResult.Success);

        var ctl = new AuthController(
            sm.Object,
            um.Object,
            Mock.Of<ITokenService>(),
            BuildConfig(),
            new Mock<IAccountService>().Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ChangePassword(new ChangePasswordDto
        {
            CurrentPassword = "old",
            NewPassword = "new"
        });

        LogResult("ChangePassword_Success", r);
        ToBool(r).Should().BeTrue();
    }

    [Fact]
    public async Task ChangePassword_Returns_False_When_CurrentPassword_Wrong()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);

        var user = new AppUser { Id = "U200", UserName = "u", Email = "u@x.com" };
        um.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        um.Setup(m => m.ChangePasswordAsync(user, "Passwordwrong", "Newpass@123"))
          .ReturnsAsync(IdentityResult.Failed(new IdentityError
          {
              Code = "PasswordMismatch",
              Description = "Incorrect password."
          }));

        var ctl = new AuthController(
            sm.Object,
            um.Object,
            Mock.Of<ITokenService>(),
            BuildConfig(),
            new Mock<IAccountService>().Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ChangePassword(new ChangePasswordDto
        {
            CurrentPassword = "Passwordwrong",
            NewPassword = "Newpass@123"
        });

        LogResult("ChangePassword_WrongPassword", r);
        ToBool(r).Should().BeFalse();
    }

    [Fact]
    public async Task ChangePassword_Returns_False_When_NewPassword_TooWeak()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);

        var user = new AppUser { Id = "U201", UserName = "weak", Email = "w@x.com" };
        um.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        um.Setup(m => m.ChangePasswordAsync(user, "Old@123", "short"))
          .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordTooShort", Description = "Too short." }));

        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), BuildConfig(), new Mock<IAccountService>().Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ChangePassword(new ChangePasswordDto { CurrentPassword = "Old@123", NewPassword = "short" });
        ToBool(r).Should().BeFalse();
    }

    [Fact]
    public async Task ChangePassword_Returns_False_When_NewPassword_FailsPolicy()
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

        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), BuildConfig(), new Mock<IAccountService>().Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ChangePassword(new ChangePasswordDto { CurrentPassword = "PasswordOld@123", NewPassword = "short" });
        ToBool(r).Should().BeFalse();
    }

    [Fact]
    public async Task ChangePassword_NewPassword_NullOrEmpty_ModelInvalid_Returns_False()
    {
        var http = new DefaultHttpContext();
        var ctl = new AuthController(
            CreateSignInManagerMock(CreateUserManagerWithOptions().Object, http).Object,
            CreateUserManagerWithOptions().Object,
            Mock.Of<ITokenService>(),
            BuildConfig(),
            new Mock<IAccountService>().Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        // NewPassword = null
        ctl.ModelState.AddModelError("NewPassword", "Required");
        var r1 = await ctl.ChangePassword(new ChangePasswordDto { CurrentPassword = "Old@123", NewPassword = null! });
        LogResult("ChangePassword_NewPassword_Null", r1);
        ToBool(r1).Should().BeFalse();

        // NewPassword = ""
        ctl.ModelState.Clear();
        ctl.ModelState.AddModelError("NewPassword", "Required");
        var r2 = await ctl.ChangePassword(new ChangePasswordDto { CurrentPassword = "Old@123", NewPassword = "" });
        LogResult("ChangePassword_NewPassword_Empty", r2);
        ToBool(r2).Should().BeFalse();
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

    // ===== Forgot password (ĐÃ SỬA: bám đúng hành vi controller: forward sang service, KHÔNG check ModelState)

    [Fact]
    public async Task ForgotPassword_Returns_Ok_Generic_Message_When_Service_Succeeds()
    {
        // Arrange
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var accounts = new Mock<IAccountService>();

        accounts.Setup(a => a.SendPasswordResetOtpAsync(It.IsAny<ForgotPasswordDto>()))
                .ReturnsAsync(new ServiceResult<ForgotPasswordDto>
                {
                    Success = true,
                    Message = "OTP has been sent to your email."
                });

        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), BuildConfig(), accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        // Act
        var r = await ctl.ForgotPassword(new ForgotPasswordDto { Email = "notfound@example.com" });
        LogResult("Forgot_Success_Generic", r);

        // Assert
        var ok = r.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new { message = "If your email is registered, you will receive an OTP." });

        accounts.Verify(a => a.SendPasswordResetOtpAsync(It.Is<ForgotPasswordDto>(d => d.Email == "notfound@example.com")), Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_Returns_BadRequest_When_Service_Fails_Email_Not_Confirmed()
    {
        // Arrange
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var accounts = new Mock<IAccountService>();

        accounts.Setup(a => a.SendPasswordResetOtpAsync(It.IsAny<ForgotPasswordDto>()))
            .ReturnsAsync(new ServiceResult<ForgotPasswordDto>
            {
                Success = false,
                Message = "Email not confirmed."
            });

        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), BuildConfig(), accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        // Act
        var r = await ctl.ForgotPassword(new ForgotPasswordDto { Email = "notconfirm@gmail.com" });

        // Assert
        var bad = r.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.Value.Should().BeEquivalentTo(new { message = "Email not confirmed." });

        accounts.Verify(a => a.SendPasswordResetOtpAsync(It.Is<ForgotPasswordDto>(d => d.Email == "notconfirm@gmail.com")), Times.Once);
    }


    [Fact]
    public async Task ForgotPassword_Returns_BadRequest_When_User_Inactive_From_Service()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var accounts = new Mock<IAccountService>();

        accounts.Setup(a => a.SendPasswordResetOtpAsync(It.IsAny<ForgotPasswordDto>()))
            .ReturnsAsync(new ServiceResult<ForgotPasswordDto>
            {
                Success = false,
                Message = "User is inactive."
            });

        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), BuildConfig(), accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ForgotPassword(new ForgotPasswordDto { Email = "inactive@gmail.com" });
        r.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ForgotPassword_ModelInvalid_Still_Calls_Service_And_Propagates_Failure()
    {
        // Arrange
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var accounts = new Mock<IAccountService>();

        accounts.Setup(a => a.SendPasswordResetOtpAsync(It.IsAny<ForgotPasswordDto>()))
                .ReturnsAsync(new ServiceResult<ForgotPasswordDto> { Success = false, Message = "Invalid email format." });

        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), BuildConfig(), accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        ctl.ModelState.AddModelError("Email", "The Email field is not a valid e-mail address.");

        // Act
        var r = await ctl.ForgotPassword(new ForgotPasswordDto { Email = "not-an-email" });

        // Assert
        var bad = r.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.Value.Should().BeEquivalentTo(new { message = "Invalid email format." });

        accounts.Verify(a => a.SendPasswordResetOtpAsync(It.Is<ForgotPasswordDto>(d => d.Email == "not-an-email")), Times.Once);
    }

    // ===== Reset password

    /*[Fact]
    public async Task ResetPassword_Returns_BadRequest_When_Model_Invalid()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var accounts = new Mock<IAccountService>();

        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), BuildConfig(), accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        ctl.ModelState.AddModelError("Email", "Required");
        var r = await ctl.ResetPassword(new ResetPasswordWithOtpDto());
        r.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ResetPassword_Returns_BadRequest_When_Service_Fails()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var accounts = new Mock<IAccountService>();

        accounts.Setup(a => a.ResetPasswordWithOtpAsync(It.IsAny<ResetPasswordWithOtpDto>()))
            .ReturnsAsync(new ServiceResult<ResetPasswordWithOtpDto> { Success = false, Message = "Invalid OTP" });

        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), BuildConfig(), accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ResetPassword(new ResetPasswordWithOtpDto { Email = "user@gmail.com" });
        r.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ResetPassword_Returns_Ok_When_Service_Succeeds()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var accounts = new Mock<IAccountService>();

        accounts.Setup(a => a.ResetPasswordWithOtpAsync(It.IsAny<ResetPasswordWithOtpDto>()))
            .ReturnsAsync(new ServiceResult<ResetPasswordWithOtpDto> { Success = true, Message = "Password has been reset successfully." });

        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), BuildConfig(), accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ResetPassword(new ResetPasswordWithOtpDto { Email = "okuser@gmail.com" });
        r.Should().BeOfType<OkObjectResult>();
    }


    [Fact]
    public async Task ResetPassword_Returns_BadRequest_When_Passwords_Do_Not_Match()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var accounts = new Mock<IAccountService>();

        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), BuildConfig(), accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        ctl.ModelState.AddModelError("ConfirmPassword", "Mật khẩu và mật khẩu xác nhận không khớp.");
        var dto = new ResetPasswordWithOtpDto
        {
            Email = "user@gmail.com",
            Otp = "123456",
            NewPassword = "NewPass@1",
            ConfirmPassword = "Different"
        };

        var r = await ctl.ResetPassword(dto);
        r.Should().BeOfType<BadRequestObjectResult>();
        accounts.Verify(a => a.ResetPasswordWithOtpAsync(It.IsAny<ResetPasswordWithOtpDto>()), Times.Never);
    }
    [Fact]
    public async Task ResetPassword_Returns_BadRequest_When_Required_Fields_Missing()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var accounts = new Mock<IAccountService>();

        var ctl = new AuthController(sm.Object, um.Object, Mock.Of<ITokenService>(), BuildConfig(), accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        ctl.ModelState.AddModelError("Otp", "Required");
        var r = await ctl.ResetPassword(new ResetPasswordWithOtpDto { Email = "user@gmail.com" });
        r.Should().BeOfType<BadRequestObjectResult>();
    }*/

}
