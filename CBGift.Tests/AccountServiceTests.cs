using System;
using System.Linq;
using System.Threading.Tasks;
using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Services;
using CB_Gift.Services.Email;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;
using Xunit.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

public class AccountServiceTests
{
    private readonly ITestOutputHelper _out;
    private static readonly JsonSerializerOptions JsonOpt = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    public AccountServiceTests(ITestOutputHelper output) => _out = output;

    private void LogResult(string label, object obj)
    {
        _out.WriteLine($"[{label}] {JsonSerializer.Serialize(obj, JsonOpt)}");
    }

    private static Mock<UserManager<AppUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<AppUser>>();
        return new Mock<UserManager<AppUser>>(
            store.Object, null, null, null, null, null, null, null, null);
    }

    // ---------- REGISTER ----------

    [Fact]
    public async Task RegisterAsync_Returns_Error_When_Email_Already_Exists()
    {
        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByEmailAsync("e@x.com"))
          .ReturnsAsync(new AppUser { Email = "e@x.com" });

        var emailSender = new Mock<IEmailSender>();
        var svc = new AccountService(um.Object, emailSender.Object);

        var rs = await svc.RegisterAsync(new RegisterRequestDto { Email = "e@x.com" });

        LogResult(nameof(RegisterAsync_Returns_Error_When_Email_Already_Exists), rs);
        rs.Success.Should().BeFalse();
        rs.Message.Should().Contain("Email already registered");
        emailSender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegisterAsync_Returns_Error_When_Create_Fails()
    {
        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByEmailAsync("new@x.com")).ReturnsAsync((AppUser)null!);
        um.Setup(m => m.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
          .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "boom" }));

        var emailSender = new Mock<IEmailSender>();
        var svc = new AccountService(um.Object, emailSender.Object);

        var rs = await svc.RegisterAsync(new RegisterRequestDto { Email = "new@x.com" });

        LogResult(nameof(RegisterAsync_Returns_Error_When_Create_Fails), rs);
        rs.Success.Should().BeFalse();
        rs.Message.Should().Contain("User creation failed");
        emailSender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegisterAsync_Succeeds_And_Sends_Welcome_Email_With_6Char_TemporaryPassword_And_EmailConfirmed()
    {
        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByEmailAsync("ok@x.com")).ReturnsAsync((AppUser)null!);
        um.Setup(m => m.CreateAsync(It.Is<AppUser>(u => u.EmailConfirmed), It.IsAny<string>()))
          .ReturnsAsync(IdentityResult.Success);

        var emailSender = new Mock<IEmailSender>();
        var svc = new AccountService(um.Object, emailSender.Object);

        var rs = await svc.RegisterAsync(new RegisterRequestDto { Email = "ok@x.com" });

        LogResult(nameof(RegisterAsync_Succeeds_And_Sends_Welcome_Email_With_6Char_TemporaryPassword_And_EmailConfirmed), rs);
        rs.Success.Should().BeTrue();
        rs.Data.Email.Should().Be("ok@x.com");
        rs.Data.TemporaryPassword.Should().NotBeNullOrEmpty();
        rs.Data.TemporaryPassword.Length.Should().Be(6);

        emailSender.Verify(es => es.SendAsync(
            "ok@x.com",
            It.Is<string>(s => s.Contains("Welcome", StringComparison.OrdinalIgnoreCase)),
            It.Is<string>(b => b.Contains("ok@x.com") && b.Contains(rs.Data.TemporaryPassword))),
            Times.Once);
    }

    // ---------- SEND RESET OTP (FORGOT PASSWORD) ----------

    [Fact]
    public async Task SendPasswordResetOtpAsync_Returns_Generic_Success_When_User_NotFound()
    {
        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByEmailAsync("nf@x.com")).ReturnsAsync((AppUser)null!);

        var emailSender = new Mock<IEmailSender>();
        var svc = new AccountService(um.Object, emailSender.Object);

        var rs = await svc.SendPasswordResetOtpAsync(new ForgotPasswordDto { Email = "nf@x.com" });

        LogResult(nameof(SendPasswordResetOtpAsync_Returns_Generic_Success_When_User_NotFound), rs);
        rs.Success.Should().BeTrue();
        rs.Message.Should().Contain("If your email is registered");
        emailSender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendPasswordResetOtpAsync_Returns_Generic_Success_When_Email_Not_Confirmed()
    {
        var user = new AppUser { Email = "u@x.com", IsActive = true };
        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByEmailAsync("u@x.com")).ReturnsAsync(user);
        um.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(false);

        var emailSender = new Mock<IEmailSender>();
        var svc = new AccountService(um.Object, emailSender.Object);

        var rs = await svc.SendPasswordResetOtpAsync(new ForgotPasswordDto { Email = "u@x.com" });

        LogResult(nameof(SendPasswordResetOtpAsync_Returns_Generic_Success_When_Email_Not_Confirmed), rs);
        rs.Success.Should().BeTrue();
        rs.Message.Should().Contain("If your email is registered");
        emailSender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendPasswordResetOtpAsync_Returns_Failure_When_User_Inactive()
    {
        var user = new AppUser { Email = "u@x.com", IsActive = false };
        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByEmailAsync("u@x.com")).ReturnsAsync(user);
        um.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(true);

        var emailSender = new Mock<IEmailSender>();
        var svc = new AccountService(um.Object, emailSender.Object);

        var rs = await svc.SendPasswordResetOtpAsync(new ForgotPasswordDto { Email = "u@x.com" });

        LogResult(nameof(SendPasswordResetOtpAsync_Returns_Failure_When_User_Inactive), rs);
        rs.Success.Should().BeFalse();
        rs.Message.Should().Contain("inactive");
        emailSender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendPasswordResetOtpAsync_Sets_Otp_And_Expiry_And_Updates_And_Sends_Email()
    {
        var user = new AppUser { Email = "u@x.com", IsActive = true };
        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByEmailAsync("u@x.com")).ReturnsAsync(user);
        um.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
        um.Setup(m => m.UpdateAsync(It.IsAny<AppUser>())).ReturnsAsync(IdentityResult.Success);

        var emailSender = new Mock<IEmailSender>();
        var svc = new AccountService(um.Object, emailSender.Object);

        var before = DateTime.UtcNow;
        var rs = await svc.SendPasswordResetOtpAsync(new ForgotPasswordDto { Email = "u@x.com" });
        var after = DateTime.UtcNow;

        LogResult(nameof(SendPasswordResetOtpAsync_Sets_Otp_And_Expiry_And_Updates_And_Sends_Email), new
        {
            rs,
            user.PasswordResetOtp,
            user.PasswordResetOtpExpiry
        });

        rs.Success.Should().BeTrue();
        user.PasswordResetOtp.Should().NotBeNullOrEmpty();
        user.PasswordResetOtp!.Length.Should().Be(6);

        user.PasswordResetOtpExpiry.Should().NotBeNull();
        // expiry = now + 2 minutes (chấp nhận sai số vài giây)
        user.PasswordResetOtpExpiry!.Value.Should().BeOnOrAfter(before.AddMinutes(1.9));
        user.PasswordResetOtpExpiry!.Value.Should().BeOnOrBefore(after.AddMinutes(2.1));

        um.Verify(m => m.UpdateAsync(It.Is<AppUser>(u =>
            u.PasswordResetOtp == user.PasswordResetOtp &&
            u.PasswordResetOtpExpiry == user.PasswordResetOtpExpiry)), Times.Once);

        emailSender.Verify(es => es.SendAsync(
            "u@x.com",
            It.Is<string>(s => s.Contains("Reset", StringComparison.OrdinalIgnoreCase)),
            It.Is<string>(b => b.Contains(user.PasswordResetOtp))), Times.Once);
    }

    // ---------- SEND RESET PASSWORD EMAIL (OTP VERSION) ----------

    [Fact]
    public async Task SendResetPasswordEmailAsync_Sends_Email_With_Otp()
    {
        var um = CreateUserManagerMock();
        var emailSender = new Mock<IEmailSender>();
        var svc = new AccountService(um.Object, emailSender.Object);

        await svc.SendResetPasswordEmailAsync("u@x.com", "123456");

        emailSender.Verify(es => es.SendAsync(
            "u@x.com",
            It.Is<string>(s => s.Contains("Reset", StringComparison.OrdinalIgnoreCase)),
            It.Is<string>(b => b.Contains("123456") && b.Contains("2 minutes"))),
            Times.Once);
    }

    // ---------- RESET PASSWORD WITH OTP ----------

    [Fact]
    public async Task ResetPasswordWithOtpAsync_Returns_Fail_When_User_Not_Found()
    {
        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByEmailAsync("nf@x.com")).ReturnsAsync((AppUser)null!);

        var emailSender = new Mock<IEmailSender>();
        var svc = new AccountService(um.Object, emailSender.Object);

        var rs = await svc.ResetPasswordWithOtpAsync(new ResetPasswordWithOtpDto
        {
            Email = "nf@x.com",
            Otp = "111111",
            NewPassword = "Abc!2345"
        });

        LogResult(nameof(ResetPasswordWithOtpAsync_Returns_Fail_When_User_Not_Found), rs);
        rs.Success.Should().BeFalse();
        rs.Message.Should().Contain("User not found");
    }

    [Fact]
    public async Task ResetPasswordWithOtpAsync_Returns_Fail_When_Otp_Invalid()
    {
        var user = new AppUser { Email = "u@x.com", PasswordResetOtp = "654321", PasswordResetOtpExpiry = DateTime.UtcNow.AddMinutes(1) };
        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByEmailAsync("u@x.com")).ReturnsAsync(user);

        var svc = new AccountService(um.Object, Mock.Of<IEmailSender>());

        var rs = await svc.ResetPasswordWithOtpAsync(new ResetPasswordWithOtpDto
        {
            Email = "u@x.com",
            Otp = "000000",
            NewPassword = "Abc!2345"
        });

        LogResult(nameof(ResetPasswordWithOtpAsync_Returns_Fail_When_Otp_Invalid), rs);
        rs.Success.Should().BeFalse();
        rs.Message.Should().Contain("Invalid OTP");
    }

    [Fact]
    public async Task ResetPasswordWithOtpAsync_Returns_Fail_When_Otp_Expired()
    {
        var user = new AppUser { Email = "u@x.com", PasswordResetOtp = "111111", PasswordResetOtpExpiry = DateTime.UtcNow.AddSeconds(-1) };
        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByEmailAsync("u@x.com")).ReturnsAsync(user);

        var svc = new AccountService(um.Object, Mock.Of<IEmailSender>());

        var rs = await svc.ResetPasswordWithOtpAsync(new ResetPasswordWithOtpDto
        {
            Email = "u@x.com",
            Otp = "111111",
            NewPassword = "Abc!2345"
        });

        LogResult(nameof(ResetPasswordWithOtpAsync_Returns_Fail_When_Otp_Expired), rs);
        rs.Success.Should().BeFalse();
        rs.Message.Should().Contain("expired");
    }

    [Fact]
    public async Task ResetPasswordWithOtpAsync_Returns_Fail_When_ResetPassword_Fails()
    {
        var user = new AppUser { Email = "u@x.com", PasswordResetOtp = "111111", PasswordResetOtpExpiry = DateTime.UtcNow.AddMinutes(1) };
        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByEmailAsync("u@x.com")).ReturnsAsync(user);
        um.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("tok");
        um.Setup(m => m.ResetPasswordAsync(user, "tok", "Abc!2345"))
          .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "weak" }));

        var svc = new AccountService(um.Object, Mock.Of<IEmailSender>());

        var rs = await svc.ResetPasswordWithOtpAsync(new ResetPasswordWithOtpDto
        {
            Email = "u@x.com",
            Otp = "111111",
            NewPassword = "Abc!2345"
        });

        LogResult(nameof(ResetPasswordWithOtpAsync_Returns_Fail_When_ResetPassword_Fails), rs);
        rs.Success.Should().BeFalse();
        rs.Message.Should().Contain("Reset password failed");
        // OTP chưa bị clear khi reset fail
        user.PasswordResetOtp.Should().Be("111111");
        user.PasswordResetOtpExpiry.Should().NotBeNull();
    }

    [Fact]
    public async Task ResetPasswordWithOtpAsync_Succeeds_Clears_Otp_And_Updates_User()
    {
        var user = new AppUser { Email = "u@x.com", PasswordResetOtp = "111111", PasswordResetOtpExpiry = DateTime.UtcNow.AddMinutes(1) };
        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByEmailAsync("u@x.com")).ReturnsAsync(user);
        um.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("tok");
        um.Setup(m => m.ResetPasswordAsync(user, "tok", "Abc!2345")).ReturnsAsync(IdentityResult.Success);
        um.Setup(m => m.UpdateAsync(It.IsAny<AppUser>())).ReturnsAsync(IdentityResult.Success);

        var svc = new AccountService(um.Object, Mock.Of<IEmailSender>());

        var rs = await svc.ResetPasswordWithOtpAsync(new ResetPasswordWithOtpDto
        {
            Email = "u@x.com",
            Otp = "111111",
            NewPassword = "Abc!2345"
        });

        LogResult(nameof(ResetPasswordWithOtpAsync_Succeeds_Clears_Otp_And_Updates_User), new
        {
            rs,
            user.PasswordResetOtp,
            user.PasswordResetOtpExpiry
        });

        rs.Success.Should().BeTrue();
        rs.Message.Should().Contain("reset successfully");
        user.PasswordResetOtp.Should().BeNull();
        user.PasswordResetOtpExpiry.Should().BeNull();

        um.Verify(m => m.UpdateAsync(It.Is<AppUser>(u => u.PasswordResetOtp == null && u.PasswordResetOtpExpiry == null)), Times.Once);
    }

    // ---------- GET ALL SELLERS ----------

    [Fact]
    public async Task GetAllSellersAsync_Returns_Mapped_Dtos()
    {
        var sellers = new List<AppUser> {
            new AppUser { Id = "S1", FullName = "Seller One" },
            new AppUser { Id = "S2", FullName = "Seller Two" }
        };

        var um = CreateUserManagerMock();
        um.Setup(m => m.GetUsersInRoleAsync("Seller")).ReturnsAsync(sellers);

        var svc = new AccountService(um.Object, Mock.Of<IEmailSender>());
        var rs = await svc.GetAllSellersAsync();

        rs.Should().HaveCount(2);
        rs.Should().ContainSingle(s => s.SellerId == "S1" && s.SellerName == "Seller One");
        rs.Should().ContainSingle(s => s.SellerId == "S2" && s.SellerName == "Seller Two");
    }
}
