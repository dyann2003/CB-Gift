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
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;
public class AuthControllerTests
{
    private IConfiguration BuildConfig() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:ExpiresMinutes"] = "60",
            ["App:ClientUrl"] = "https://example.com"
        }).Build();

    private static Mock<UserManager<AppUser>> CreateUserManagerWithOptions()
    {
        var store = new Mock<IUserStore<AppUser>>();
        var options = Options.Create(new IdentityOptions());     // <-- cần cái này
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

    [Fact]
    public async Task Login_Returns_Unauthorized_When_User_NotFound_Or_Inactive()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var tokens = new Mock<ITokenService>();
        var accounts = new Mock<IAccountService>();
        var config = BuildConfig();

        // user not found: cả FindByName + FindByEmail đều null
        um.Setup(m => m.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((AppUser)null!);
        um.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((AppUser)null!);

        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r1 = await ctl.Login(new LoginDto { UserNameOrEmail = "nope", Password = "x" });
        r1.Should().BeOfType<UnauthorizedObjectResult>();

        // inactive user
        var inactive = new AppUser { UserName = "u", Email = "u@x.com", IsActive = false };
        um.Setup(m => m.FindByNameAsync("u")).ReturnsAsync(inactive);
        var r2 = await ctl.Login(new LoginDto { UserNameOrEmail = "u", Password = "x" });
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

        var user = new AppUser { UserName = "u2", Email = "u2@x.com", IsActive = true };
        um.Setup(m => m.FindByNameAsync("u2")).ReturnsAsync(user);
        um.Setup(m => m.FindByEmailAsync("u2")).ReturnsAsync((AppUser)null!);

        sm.Setup(s => s.CheckPasswordSignInAsync(user, "bad", false))
          .ReturnsAsync(IdentitySignInResult.Failed);

        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.Login(new LoginDto { UserNameOrEmail = "u2", Password = "bad" });
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

        sm.Setup(s => s.CheckPasswordSignInAsync(user, "good", false))
          .ReturnsAsync(IdentitySignInResult.Success);
        tokens.Setup(t => t.CreateTokenAsync(user)).ReturnsAsync("jwt-token");

        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var result = await ctl.Login(new LoginDto { UserNameOrEmail = "okuser", Password = "good" });
        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();


        http.Response.Headers.TryGetValue("Set-Cookie", out var setCookies).Should().BeTrue();
        setCookies.ToString().Should().Contain("access_token=");
    }
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
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ChangePassword_Returns_Unauthorized_When_User_Not_In_Context()
    {
        var http = new DefaultHttpContext(); // không set ClaimsPrincipal => null user
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var tokens = new Mock<ITokenService>();
        var accounts = new Mock<IAccountService>();
        var config = BuildConfig();

        // Khi GetUserAsync(User) chạy, extension sẽ cố FindByIdAsync(null) -> ta không setup => trả null
        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ChangePassword(new ChangePasswordDto { CurrentPassword = "a", NewPassword = "b" });
        r.Should().BeOfType<UnauthorizedResult>();
    }
    [Fact]
    public async Task ChangePassword_Returns_BadRequest_When_Manager_Fails()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();                      // mock cũ cũng được
        var sm = CreateSignInManagerMock(um.Object, http);
        var tokens = new Mock<ITokenService>();
        var accounts = new Mock<IAccountService>();
        var config = BuildConfig();

        var user = new AppUser { Id = "U100", UserName = "test", Email = "t@x.com" };

        // Quan trọng: stub trực tiếp GetUserAsync để controller nhận được user
        um.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
          .ReturnsAsync(user);

        um.Setup(m => m.ChangePasswordAsync(user, "old", "new"))
          .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "E1", Description = "boom" }));

        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ChangePassword(new ChangePasswordDto { CurrentPassword = "old", NewPassword = "new" });
        r.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ChangePassword_Returns_Ok_On_Success()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var tokens = new Mock<ITokenService>();
        var accounts = new Mock<IAccountService>();
        var config = BuildConfig();

        var user = new AppUser { Id = "U101", UserName = "ok", Email = "ok@x.com" };

        // Stub thẳng GetUserAsync để bỏ qua claim/options
        um.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
          .ReturnsAsync(user);

        um.Setup(m => m.ChangePasswordAsync(user, "old", "new"))
          .ReturnsAsync(IdentityResult.Success);

        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ChangePassword(new ChangePasswordDto { CurrentPassword = "old", NewPassword = "new" });
        r.Should().BeOfType<OkObjectResult>();
    }
    [Fact]
    public async Task Register_Returns_BadRequest_When_AccountService_Fails()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var tokens = new Mock<ITokenService>();
        var accounts = new Mock<IAccountService>();
        var config = BuildConfig();

        accounts.Setup(a => a.RegisterAsync(It.IsAny<RegisterRequestDto>()))
                .ReturnsAsync(new ServiceResult<RegisterResponseDto>
                {
                    Success = false,
                    Message = "Email already registered."
                });

        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.Register(new RegisterRequestDto { Email = "dup@x.com" });
        r.Should().BeOfType<BadRequestObjectResult>();
    }
    [Fact]
    public async Task Register_Returns_Ok_With_Data_On_Success()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var tokens = new Mock<ITokenService>();
        var accounts = new Mock<IAccountService>();
        var config = BuildConfig();

        var data = new RegisterResponseDto { Email = "new@x.com", TemporaryPassword = "abc123" };
        accounts.Setup(a => a.RegisterAsync(It.IsAny<RegisterRequestDto>()))
                .ReturnsAsync(new ServiceResult<RegisterResponseDto>
                {
                    Success = true,
                    Data = data
                });

        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.Register(new RegisterRequestDto { Email = "new@x.com" });
        r.Should().BeOfType<OkObjectResult>();
    }
    [Fact]
    public async Task ForgotPassword_Returns_BadRequest_When_User_NotFound()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var tokens = new Mock<ITokenService>();
        var accounts = new Mock<IAccountService>();
        var config = BuildConfig();

        um.Setup(m => m.FindByEmailAsync("none@x.com")).ReturnsAsync((AppUser)null!);

        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ForgotPassword(new ForgotPasswordDto { Email = "none@x.com" });
        r.Should().BeOfType<BadRequestObjectResult>();
    }
    [Fact]
    public async Task ForgotPassword_Returns_BadRequest_When_Email_Not_Confirmed()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var tokens = new Mock<ITokenService>();
        var accounts = new Mock<IAccountService>();
        var config = BuildConfig();

        var user = new AppUser { Email = "u@x.com" };
        um.Setup(m => m.FindByEmailAsync("u@x.com")).ReturnsAsync(user);
        um.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(false);

        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ForgotPassword(new ForgotPasswordDto { Email = "u@x.com" });
        r.Should().BeOfType<BadRequestObjectResult>();
    }
    [Fact]
    public async Task ForgotPassword_Sends_Reset_Email_And_Returns_Ok()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var tokens = new Mock<ITokenService>();
        var accounts = new Mock<IAccountService>();
        var config = BuildConfig(); // có App:ClientUrl

        var user = new AppUser { Email = "u@x.com" };
        um.Setup(m => m.FindByEmailAsync("u@x.com")).ReturnsAsync(user);
        um.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
        um.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("tok123");

        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ForgotPassword(new ForgotPasswordDto { Email = "u@x.com" });
        r.Should().BeOfType<OkObjectResult>();

        accounts.Verify(a => a.SendResetPasswordEmailAsync(
            "u@x.com",
            It.Is<string>(link => link.Contains("reset-password") && link.Contains("tok123"))),
            Times.Once);
    }
    [Fact]
    public async Task ResetPassword_Returns_BadRequest_When_User_NotFound()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var tokens = new Mock<ITokenService>();
        var accounts = new Mock<IAccountService>();
        var config = BuildConfig();

        um.Setup(m => m.FindByEmailAsync("none@x.com")).ReturnsAsync((AppUser)null!);

        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ResetPassword(new ResetPasswordDto { Email = "none@x.com", Token = "t", NewPassword = "P@ss1" });
        r.Should().BeOfType<BadRequestObjectResult>();
    }
    [Fact]
    public async Task ResetPassword_Returns_BadRequest_When_Reset_Fails()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var tokens = new Mock<ITokenService>();
        var accounts = new Mock<IAccountService>();
        var config = BuildConfig();

        var user = new AppUser { Email = "u@x.com" };
        um.Setup(m => m.FindByEmailAsync("u@x.com")).ReturnsAsync(user);
        um.Setup(m => m.ResetPasswordAsync(user, "tok", "NewP@ss1"))
          .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "invalid token" }));

        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ResetPassword(new ResetPasswordDto { Email = "u@x.com", Token = "tok", NewPassword = "NewP@ss1" });
        r.Should().BeOfType<BadRequestObjectResult>();
    }
    [Fact]
    public async Task ResetPassword_Returns_Ok_On_Success()
    {
        var http = new DefaultHttpContext();
        var um = CreateUserManagerWithOptions();
        var sm = CreateSignInManagerMock(um.Object, http);
        var tokens = new Mock<ITokenService>();
        var accounts = new Mock<IAccountService>();
        var config = BuildConfig();

        var user = new AppUser { Email = "ok@x.com" };
        um.Setup(m => m.FindByEmailAsync("ok@x.com")).ReturnsAsync(user);
        um.Setup(m => m.ResetPasswordAsync(user, "tok", "NewP@ss1")).ReturnsAsync(IdentityResult.Success);

        var ctl = new AuthController(sm.Object, um.Object, tokens.Object, config, accounts.Object)
        { ControllerContext = new ControllerContext { HttpContext = http } };

        var r = await ctl.ResetPassword(new ResetPasswordDto { Email = "ok@x.com", Token = "tok", NewPassword = "NewP@ss1" });
        r.Should().BeOfType<OkObjectResult>();
    }

}
