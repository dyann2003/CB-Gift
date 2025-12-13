using CB_Gift.Controllers;
using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Models;
using CB_Gift.Services.Email;
using CB_Gift.Services.IService;
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

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:ExpiresMinutes"] = "60",
            ["App:ClientUrl"] = "https://example.com"
        }).Build();

    private static Mock<UserManager<AppUser>> CreateUserManager()
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

    private static Mock<SignInManager<AppUser>> CreateSignInManager(UserManager<AppUser> um, HttpContext? ctx = null)
    {
        return new Mock<SignInManager<AppUser>>(
            um,
            new HttpContextAccessor { HttpContext = ctx ?? new DefaultHttpContext() },
            Mock.Of<IUserClaimsPrincipalFactory<AppUser>>(),
            null, null, null, null)
        { CallBase = true };
    }

    private static (AuthController ctl,
        DefaultHttpContext http,
        Mock<UserManager<AppUser>> um,
        Mock<SignInManager<AppUser>> sm,
        Mock<ITokenService> tokens,
        Mock<IAccountService> accounts,
        Mock<IInvoiceService> invoice,
        IConfiguration config) BuildSut()
    {
        var http = new DefaultHttpContext();

        var um = CreateUserManager();
        var sm = CreateSignInManager(um.Object, http);

        var tokens = new Mock<ITokenService>(MockBehavior.Strict);
        var accounts = new Mock<IAccountService>(MockBehavior.Strict);
        var invoice = new Mock<IInvoiceService>(MockBehavior.Strict);

        var config = BuildConfig();

        var ctl = new AuthController(
            sm.Object,
            um.Object,
            tokens.Object,
            config,
            accounts.Object,
            invoice.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };

        return (ctl, http, um, sm, tokens, accounts, invoice, config);
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

    private static string? ExtractMessage(IActionResult result)
    {
        if (result is OkObjectResult ok && ok.Value != null)
        {
            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("message", out var msg)) return msg.GetString();
        }
        if (result is UnauthorizedObjectResult un && un.Value != null)
        {
            var json = JsonSerializer.Serialize(un.Value);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("message", out var msg)) return msg.GetString();
        }
        if (result is BadRequestObjectResult bad && bad.Value != null)
        {
            var json = JsonSerializer.Serialize(bad.Value);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("message", out var msg)) return msg.GetString();
        }
        return null;
    }

    private static void AssertSetCookieContains(DefaultHttpContext http, params string[] cookieKeys)
    {
        http.Response.Headers.TryGetValue("Set-Cookie", out var setCookies).Should().BeTrue();
        var raw = setCookies.ToString();
        foreach (var key in cookieKeys)
            raw.Should().Contain($"{key}=");
    }

    // =========================
    // LOGIN (cover theo bảng: username/email variations + password normal/boundary + invalid)
    // =========================

    [Fact]
    public async Task Login_UTCID01_Success_ReturnsTokens_And_SetsCookies()
    {
        var (ctl, http, um, sm, tokens, accounts, invoice, _) = BuildSut();

        var user = new AppUser { Id = "U1", UserName = "test@example.com", Email = "test@example.com", IsActive = true };

        um.Setup(x => x.FindByNameAsync("test@example.com")).ReturnsAsync((AppUser)null!);
        um.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync(user);

        sm.Setup(x => x.CheckPasswordSignInAsync(user, "Password_Pass@123", false))
          .ReturnsAsync(IdentitySignInResult.Success);

        tokens.Setup(t => t.CreateTokenAsync(user)).ReturnsAsync("access.jwt");
        tokens.Setup(t => t.GenerateRefreshTokenAsync(user.Id))
              .ReturnsAsync(new RefreshToken { Token = "refresh.jwt" });


        var r = await ctl.Login(new LoginDto
        {
            UserNameOrEmail = "test@example.com",
            Password = "Password_Pass@123"
        });

        LogResult("Login_UTCID01", r);

        var ok = r.Should().BeOfType<OkObjectResult>().Subject;

        // cookies
        AssertSetCookieContains(http, "access_token", "refresh_token");

        // response payload
        var json = JsonSerializer.Serialize(ok.Value);
        json.Should().Contain("AccessToken");
        json.Should().Contain("RefreshToken");

        tokens.VerifyAll();
        sm.VerifyAll();
        um.VerifyAll();
        accounts.VerifyNoOtherCalls();
        invoice.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Login_UTCID02_UserNotFound_Returns401_WithMessage()
    {
        var (ctl, _, um, sm, tokens, accounts, invoice, _) = BuildSut();

        um.Setup(x => x.FindByNameAsync("notfound@example.com")).ReturnsAsync((AppUser)null!);
        um.Setup(x => x.FindByEmailAsync("notfound@example.com")).ReturnsAsync((AppUser)null!);

        // nothing else should be called
        var r = await ctl.Login(new LoginDto { UserNameOrEmail = "notfound@example.com", Password = "Password_Pass@123" });
        LogResult("Login_UTCID02", r);

        r.Should().BeOfType<UnauthorizedObjectResult>();
        ExtractMessage(r).Should().Be("User not found.");

        sm.Verify(x => x.CheckPasswordSignInAsync(It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        tokens.VerifyNoOtherCalls();
        accounts.VerifyNoOtherCalls();
        invoice.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Login_UTCID06_InactiveUser_Returns401_WithMessage()
    {
        var (ctl, _, um, sm, tokens, accounts, invoice, _) = BuildSut();

        var inactive = new AppUser { Id = "U2", UserName = "inactive@gmail.com", Email = "inactive@gmail.com", IsActive = false };

        um.Setup(x => x.FindByNameAsync("inactive@gmail.com")).ReturnsAsync((AppUser)null!);
        um.Setup(x => x.FindByEmailAsync("inactive@gmail.com")).ReturnsAsync(inactive);

        var r = await ctl.Login(new LoginDto { UserNameOrEmail = "inactive@gmail.com", Password = "Password_Pass@123" });
        LogResult("Login_UTCID06", r);

        r.Should().BeOfType<UnauthorizedObjectResult>();
        ExtractMessage(r).Should().Be("Account deactivated.");

        sm.Verify(x => x.CheckPasswordSignInAsync(It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        tokens.VerifyNoOtherCalls();
        accounts.VerifyNoOtherCalls();
        invoice.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Login_UTCID10_InvalidCredentials_Returns401_WithMessage()
    {
        var (ctl, _, um, sm, tokens, accounts, invoice, _) = BuildSut();

        var user = new AppUser { Id = "U3", UserName = "test@example.com", Email = "test@example.com", IsActive = true };

        um.Setup(x => x.FindByNameAsync("test@example.com")).ReturnsAsync((AppUser)null!);
        um.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync(user);

        sm.Setup(x => x.CheckPasswordSignInAsync(user, "password_test_fail", false))
          .ReturnsAsync(IdentitySignInResult.Failed);

        var r = await ctl.Login(new LoginDto { UserNameOrEmail = "test@example.com", Password = "password_test_fail" });
        LogResult("Login_UTCID10", r);

        r.Should().BeOfType<UnauthorizedObjectResult>();
        ExtractMessage(r).Should().Be("Invalid credentials.");

        tokens.VerifyNoOtherCalls();
        accounts.VerifyNoOtherCalls();
        invoice.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("short")] // UTCID08 (Boundary lower than 8)
    [InlineData("L/g0R)a[F];c:H.v?6E<X>z\"")] // UTCID09 (dài bất thường)
    public async Task Login_BoundaryPassword_StillHandled_ByController(string password)
    {
        // Controller không tự validate length; nếu DTO có [StringLength] thì thường fail ModelState trong runtime.
        // Ở unit test, để match bảng, ta chủ động add ModelState error (giống khi framework validate).
        var (ctl, _, um, sm, tokens, accounts, invoice, _) = BuildSut();

        ctl.ModelState.AddModelError("Password", "The password must be 8 - 50 characters");

        var r = await ctl.Login(new LoginDto { UserNameOrEmail = "test@example.com", Password = password });
        LogResult("Login_BoundaryPassword_ModelState", r);

        r.Should().BeOfType<BadRequestObjectResult>();

        // Ensure no downstream calls
        um.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Never);
        um.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        sm.Verify(x => x.CheckPasswordSignInAsync(It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);

        tokens.VerifyNoOtherCalls();
        accounts.VerifyNoOtherCalls();
        invoice.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Login_UTCID04_UsernameRequired_ModelStateBadRequest(string? uoe)
    {
        var (ctl, _, um, sm, tokens, accounts, invoice, _) = BuildSut();

        ctl.ModelState.AddModelError("UserNameOrEmail", "The Email field is required.");

        var r = await ctl.Login(new LoginDto { UserNameOrEmail = uoe!, Password = "Password_Pass@123" });
        LogResult("Login_UTCID04", r);

        r.Should().BeOfType<BadRequestObjectResult>();

        um.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Never);
        um.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        sm.Verify(x => x.CheckPasswordSignInAsync(It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);

        tokens.VerifyNoOtherCalls();
        accounts.VerifyNoOtherCalls();
        invoice.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Login_UTCID05_InvalidEmailFormat_ModelStateBadRequest()
    {
        var (ctl, _, um, sm, tokens, accounts, invoice, _) = BuildSut();

        ctl.ModelState.AddModelError("UserNameOrEmail", "The Email field is not a valid e-mail address.");

        var r = await ctl.Login(new LoginDto { UserNameOrEmail = "notanemail@dcd", Password = "Password_Pass@123" });
        LogResult("Login_UTCID05", r);

        r.Should().BeOfType<BadRequestObjectResult>();

        um.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Never);
        um.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        sm.Verify(x => x.CheckPasswordSignInAsync(It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);

        tokens.VerifyNoOtherCalls();
        accounts.VerifyNoOtherCalls();
        invoice.VerifyNoOtherCalls();
    }

    // =========================
    // FORGOT PASSWORD (cover theo bảng: success, notfound, invalid email, inactive, null)
    // =========================

    [Fact]
    public async Task Forgot_UTCID01_Success_ReturnsOk_And_RevokesAllRefreshTokens()
    {
        var (ctl, _, um, sm, tokens, accounts, invoice, _) = BuildSut();

        var user = new AppUser { Id = "U10", UserName = "test@example.com", Email = "test@example.com", IsActive = true };

        um.Setup(x => x.FindByNameAsync("test@example.com")).ReturnsAsync((AppUser)null!);
        um.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync(user);

        accounts.Setup(a => a.SendPasswordResetOtpAsync(It.Is<ForgotPasswordDto>(d => d.Email == "test@example.com")))
                .ReturnsAsync(new ServiceResult<ForgotPasswordDto> { Success = true, Message = "OTP sent." });

        tokens.Setup(t => t.RevokeAllRefreshTokensAsync(user.Id)).Returns(Task.CompletedTask);

        var r = await ctl.ForgotPassword(new ForgotPasswordDto { Email = "test@example.com" });
        LogResult("Forgot_UTCID01", r);

        r.Should().BeOfType<OkObjectResult>();
        ExtractMessage(r).Should().Be("OTP sent.");

        accounts.VerifyAll();
        tokens.VerifyAll();

        // not used
        sm.VerifyNoOtherCalls();
        invoice.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Forgot_UTCID02_UserNotFound_Returns401()
    {
        var (ctl, _, um, sm, tokens, accounts, invoice, _) = BuildSut();

        um.Setup(x => x.FindByNameAsync("notfound@example.com")).ReturnsAsync((AppUser)null!);
        um.Setup(x => x.FindByEmailAsync("notfound@example.com")).ReturnsAsync((AppUser)null!);

        var r = await ctl.ForgotPassword(new ForgotPasswordDto { Email = "notfound@example.com" });
        LogResult("Forgot_UTCID02", r);

        r.Should().BeOfType<UnauthorizedObjectResult>();
        ExtractMessage(r).Should().Be("Email not found.");

        accounts.VerifyNoOtherCalls();
        tokens.VerifyNoOtherCalls();
        sm.VerifyNoOtherCalls();
        invoice.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Forgot_UTCID05_Inactive_Returns401()
    {
        var (ctl, _, um, sm, tokens, accounts, invoice, _) = BuildSut();

        var user = new AppUser { Id = "U11", UserName = "inactive@gmail.com", Email = "inactive@gmail.com", IsActive = false };

        um.Setup(x => x.FindByNameAsync("inactive@gmail.com")).ReturnsAsync((AppUser)null!);
        um.Setup(x => x.FindByEmailAsync("inactive@gmail.com")).ReturnsAsync(user);

        var r = await ctl.ForgotPassword(new ForgotPasswordDto { Email = "inactive@gmail.com" });
        LogResult("Forgot_UTCID05", r);

        r.Should().BeOfType<UnauthorizedObjectResult>();
        ExtractMessage(r).Should().Be("Your account has been deactivated.");

        accounts.VerifyNoOtherCalls();
        tokens.VerifyNoOtherCalls();
        sm.VerifyNoOtherCalls();
        invoice.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Forgot_UTCID06_EmailRequired_Returns400(string? email)
    {
        var (ctl, _, um, sm, tokens, accounts, invoice, _) = BuildSut();

        var r = await ctl.ForgotPassword(new ForgotPasswordDto { Email = email! });
        LogResult("Forgot_UTCID06", r);

        r.Should().BeOfType<BadRequestObjectResult>();
        ExtractMessage(r).Should().Be("Please enter your email address.");

        um.Verify(x => x.FindByNameAsync(It.IsAny<string>()), Times.Never);
        um.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never);

        accounts.VerifyNoOtherCalls();
        tokens.VerifyNoOtherCalls();
        sm.VerifyNoOtherCalls();
        invoice.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Forgot_InvalidEmailFormat_CanBeHandled_ByService_Failure()
    {
        // Nếu bạn muốn đúng theo bảng: invalid email => "The Email field is not a valid e-mail address."
        // Controller hiện không check ModelState, nên ta mock service trả fail message đó.
        var (ctl, _, um, sm, tokens, accounts, invoice, _) = BuildSut();

        var user = new AppUser { Id = "U12", UserName = "notanemail@dcd", Email = "notanemail@dcd", IsActive = true };

        um.Setup(x => x.FindByNameAsync("notanemail@dcd")).ReturnsAsync((AppUser)null!);
        um.Setup(x => x.FindByEmailAsync("notanemail@dcd")).ReturnsAsync(user);

        accounts.Setup(a => a.SendPasswordResetOtpAsync(It.Is<ForgotPasswordDto>(d => d.Email == "notanemail@dcd")))
                .ReturnsAsync(new ServiceResult<ForgotPasswordDto> { Success = false, Message = "The Email field is not a valid e-mail address." });

        var r = await ctl.ForgotPassword(new ForgotPasswordDto { Email = "notanemail@dcd" });
        LogResult("Forgot_InvalidEmailFormat", r);

        r.Should().BeOfType<BadRequestObjectResult>();
        ExtractMessage(r).Should().Be("The Email field is not a valid e-mail address.");

        // because fail => should NOT revoke tokens
        tokens.Verify(t => t.RevokeAllRefreshTokensAsync(It.IsAny<string>()), Times.Never);

        accounts.VerifyAll();
        sm.VerifyNoOtherCalls();
        invoice.VerifyNoOtherCalls();
    }

    // =========================
    // RESET PASSWORD WITH OTP (cover theo bảng: required fields, otp length, notfound, expired/incorrect otp, password policy, mismatch, boundary)
    // =========================

    [Fact]
    public async Task Reset_UTCID01_Success_ReturnsOk_Message()
    {
        var (ctl, _, um, sm, tokens, accounts, invoice, _) = BuildSut();

        accounts.Setup(a => a.ResetPasswordWithOtpAsync(It.Is<ResetPasswordWithOtpDto>(d =>
                d.Email == "test@example.com" &&
                d.Otp == "123456" &&
                d.NewPassword == "Password_Pass@123" &&
                d.ConfirmPassword == "Password_Pass@123")))
            .ReturnsAsync(new ServiceResult<ResetPasswordWithOtpDto> { Success = true, Message = "Password reset success." });

        var dto = new ResetPasswordWithOtpDto
        {
            Email = "test@example.com",
            Otp = "123456",
            NewPassword = "Password_Pass@123",
            ConfirmPassword = "Password_Pass@123"
        };

        var r = await ctl.ResetPasswordWithOtp(dto);
        LogResult("Reset_UTCID01", r);

        r.Should().BeOfType<OkObjectResult>();
        ExtractMessage(r).Should().Be("Password reset success.");

        accounts.VerifyAll();
        um.VerifyNoOtherCalls();
        sm.VerifyNoOtherCalls();
        tokens.VerifyNoOtherCalls();
        invoice.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Reset_EmailRequired_ModelStateBadRequest()
    {
        var (ctl, _, um, sm, tokens, accounts, invoice, _) = BuildSut();
        ctl.ModelState.AddModelError("Email", "The Email field is required.");

        var r = await ctl.ResetPasswordWithOtp(new ResetPasswordWithOtpDto
        {
            Email = null!,
            Otp = "123456",
            NewPassword = "Password_Pass@123",
            ConfirmPassword = "Password_Pass@123"
        });

        LogResult("Reset_EmailRequired", r);

        r.Should().BeOfType<BadRequestObjectResult>();
        accounts.VerifyNoOtherCalls();
        um.VerifyNoOtherCalls();
        sm.VerifyNoOtherCalls();
        tokens.VerifyNoOtherCalls();
        invoice.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("12345")]   // < 6 digits
    [InlineData("1234567")] // > 6 digits
    public async Task Reset_OtpMustBe6Digits_ModelStateBadRequest(string otp)
    {
        var (ctl, _, um, sm, tokens, accounts, invoice, _) = BuildSut();
        ctl.ModelState.AddModelError("Otp", "The otp must be 6 digits.");

        var r = await ctl.ResetPasswordWithOtp(new ResetPasswordWithOtpDto
        {
            Email = "test@example.com",
            Otp = otp,
            NewPassword = "Password_Pass@123",
            ConfirmPassword = "Password_Pass@123"
        });

        LogResult("Reset_OtpMustBe6Digits", r);

        r.Should().BeOfType<BadRequestObjectResult>();
        accounts.VerifyNoOtherCalls();
        um.VerifyNoOtherCalls();
        sm.VerifyNoOtherCalls();
        tokens.VerifyNoOtherCalls();
        invoice.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("Reset_UserNotFound")]
    [InlineData("Reset_ExpiredOtp")]
    [InlineData("Reset_IncorrectOTP")]
    [InlineData("Reset_InvalidPassword")]
    [InlineData("Reset_NotMatchPassword")]
    public async Task Reset_ServiceFail_PropagatesMessage_AsBadRequest(string message)
    {
        var (ctl, _, um, sm, tokens, accounts, invoice, _) = BuildSut();

        accounts.Setup(a => a.ResetPasswordWithOtpAsync(It.IsAny<ResetPasswordWithOtpDto>()))
            .ReturnsAsync(new ServiceResult<ResetPasswordWithOtpDto> { Success = false, Message = message });

        var dto = new ResetPasswordWithOtpDto
        {
            Email = "test@example.com",
            Otp = "123456",
            NewPassword = "Password_Pass@123",
            ConfirmPassword = "Password_Pass@123"
        };

        var r = await ctl.ResetPasswordWithOtp(dto);
        LogResult($"Reset_Fail_{message}", r);

        r.Should().BeOfType<BadRequestObjectResult>();
        ExtractMessage(r).Should().Be(message);

        accounts.VerifyAll();
        um.VerifyNoOtherCalls();
        sm.VerifyNoOtherCalls();
        tokens.VerifyNoOtherCalls();
        invoice.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("Abcdef12!")] // Boundary min ~ 8
    [InlineData("Abcdef12!Abcdef12!Abcdef12!Abcdef12!Abcdef12!")] // ~ 50
    public async Task Reset_BoundaryPassword_Valid_WhenServiceReturnsSuccess(string pwd)
    {
        var (ctl, _, um, sm, tokens, accounts, invoice, _) = BuildSut();

        accounts.Setup(a => a.ResetPasswordWithOtpAsync(It.Is<ResetPasswordWithOtpDto>(d => d.NewPassword == pwd && d.ConfirmPassword == pwd)))
            .ReturnsAsync(new ServiceResult<ResetPasswordWithOtpDto> { Success = true, Message = "OK" });

        var r = await ctl.ResetPasswordWithOtp(new ResetPasswordWithOtpDto
        {
            Email = "test@example.com",
            Otp = "123456",
            NewPassword = pwd,
            ConfirmPassword = pwd
        });

        LogResult("Reset_BoundaryPwd", r);

        r.Should().BeOfType<OkObjectResult>();
        ExtractMessage(r).Should().Be("OK");

        accounts.VerifyAll();
        um.VerifyNoOtherCalls();
        sm.VerifyNoOtherCalls();
        tokens.VerifyNoOtherCalls();
        invoice.VerifyNoOtherCalls();
    }

    // =========================
    // CHANGE PASSWORD (cover theo bảng: success, wrong current, required fields, policy fail, boundary)
    // =========================

    private static ClaimsPrincipal BuildPrincipal(string userId = "U200")
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, "tester")
        }, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task ChangePassword_UTCID01_Success_RevokesAllTokens_DeletesCookies_ReturnsOk()
    {
        var (ctl, http, um, sm, tokens, accounts, invoice, _) = BuildSut();

        var user = new AppUser { Id = "U200", UserName = "u", Email = "u@x.com", IsActive = true };

        // ensure controller has User in context
        http.User = BuildPrincipal(user.Id);

        um.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        um.Setup(x => x.ChangePasswordAsync(user, "Current_Password@123", "New_Password_Pass@123"))
          .ReturnsAsync(IdentityResult.Success);

        tokens.Setup(t => t.RevokeAllRefreshTokensAsync(user.Id)).Returns(Task.CompletedTask);

        var r = await ctl.ChangePassword(new ChangePasswordDto
        {
            CurrentPassword = "Current_Password@123",
            NewPassword = "New_Password_Pass@123"
        });

        LogResult("Change_UTCID01", r);

        r.Should().BeOfType<OkObjectResult>();
        ExtractMessage(r).Should().Be("Password changed. All devices have been logged out.");

        // cookies deleted => Set-Cookie usually exists
        http.Response.Headers.TryGetValue("Set-Cookie", out var setCookies).Should().BeTrue();
        setCookies.ToString().Should().Contain("access_token=");
        setCookies.ToString().Should().Contain("refresh_token=");

        um.VerifyAll();
        tokens.VerifyAll();
        sm.VerifyNoOtherCalls();
        accounts.VerifyNoOtherCalls();
        invoice.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ChangePassword_Unauthorized_WhenUserNull()
    {
        var (ctl, http, um, sm, tokens, accounts, invoice, _) = BuildSut();
        http.User = BuildPrincipal("U404");

        um.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((AppUser)null!);

        var r = await ctl.ChangePassword(new ChangePasswordDto
        {
            CurrentPassword = "x",
            NewPassword = "y"
        });

        LogResult("Change_Unauthorized_UserNull", r);

        r.Should().BeOfType<UnauthorizedResult>();

        tokens.VerifyNoOtherCalls();
        accounts.VerifyNoOtherCalls();
        invoice.VerifyNoOtherCalls();
        sm.VerifyNoOtherCalls();
        um.VerifyAll();
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ReturnsBadRequest_WithErrors()
    {
        var (ctl, http, um, sm, tokens, accounts, invoice, _) = BuildSut();

        var user = new AppUser { Id = "U201", UserName = "u", Email = "u@x.com" };
        http.User = BuildPrincipal(user.Id);

        um.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        um.Setup(x => x.ChangePasswordAsync(user, "Wrong_Current_Password@123", "New_Password_Pass@123"))
          .ReturnsAsync(IdentityResult.Failed(new IdentityError
          {
              Code = "PasswordMismatch",
              Description = "Incorrect password."
          }));

        var r = await ctl.ChangePassword(new ChangePasswordDto
        {
            CurrentPassword = "Wrong_Current_Password@123",
            NewPassword = "New_Password_Pass@123"
        });

        LogResult("Change_WrongCurrent", r);

        var bad = r.Should().BeOfType<BadRequestObjectResult>().Subject;
        var json = JsonSerializer.Serialize(bad.Value);
        json.Should().Contain("Change password failed");
        json.Should().Contain("PasswordMismatch");
        json.Should().Contain("Incorrect password.");

        // on failure => should NOT revoke all tokens
        tokens.Verify(t => t.RevokeAllRefreshTokensAsync(It.IsAny<string>()), Times.Never);

        um.VerifyAll();
        sm.VerifyNoOtherCalls();
        accounts.VerifyNoOtherCalls();
        invoice.VerifyNoOtherCalls();
        tokens.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ChangePassword_ModelStateInvalid_ReturnsBadRequest()
    {
        var (ctl, http, um, sm, tokens, accounts, invoice, _) = BuildSut();
        http.User = BuildPrincipal("U202");

        ctl.ModelState.AddModelError("NewPassword", "The password must be 8 - 50 characters");

        var r = await ctl.ChangePassword(new ChangePasswordDto
        {
            CurrentPassword = "Current_Password@123",
            NewPassword = "short"
        });

        LogResult("Change_ModelStateInvalid", r);

        r.Should().BeOfType<BadRequestObjectResult>();

        um.Verify(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Never);
        um.Verify(x => x.ChangePasswordAsync(It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        tokens.VerifyNoOtherCalls();
        accounts.VerifyNoOtherCalls();
        invoice.VerifyNoOtherCalls();
        sm.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("Abcdef12!")] // boundary min
    [InlineData("Abcdef12!Abcdef12!Abcdef12!Abcdef12!Abcdef12!")] // boundary max-ish
    public async Task ChangePassword_BoundaryPasswords_Success(string newPwd)
    {
        var (ctl, http, um, sm, tokens, accounts, invoice, _) = BuildSut();

        var user = new AppUser { Id = "U203", UserName = "u", Email = "u@x.com" };
        http.User = BuildPrincipal(user.Id);

        um.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        um.Setup(x => x.ChangePasswordAsync(user, "Current_Password@123", newPwd))
          .ReturnsAsync(IdentityResult.Success);

        tokens.Setup(t => t.RevokeAllRefreshTokensAsync(user.Id)).Returns(Task.CompletedTask);

        var r = await ctl.ChangePassword(new ChangePasswordDto
        {
            CurrentPassword = "Current_Password@123",
            NewPassword = newPwd
        });

        LogResult("Change_Boundary", r);

        r.Should().BeOfType<OkObjectResult>();
        ExtractMessage(r).Should().Be("Password changed. All devices have been logged out.");

        um.VerifyAll();
        tokens.VerifyAll();

        sm.VerifyNoOtherCalls();
        accounts.VerifyNoOtherCalls();
        invoice.VerifyNoOtherCalls();
    }
}
