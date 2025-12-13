using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Services;
using CB_Gift.Services.Email;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Abstractions;

public class AccountServiceTests
{
    private readonly ITestOutputHelper _out;

    private static readonly JsonSerializerOptions JsonOpt = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AccountServiceTests(ITestOutputHelper output) => _out = output;

    private void LogResult(string label, object obj)
        => _out.WriteLine($"[{label}] {JsonSerializer.Serialize(obj, JsonOpt)}");

    private static Mock<UserManager<AppUser>> CreateUserManagerMock()
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

    [Fact]
    public async Task RegisterAsync_Returns_Error_When_Email_Already_Exists()
    {
        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByEmailAsync("e@x.com"))
          .ReturnsAsync(new AppUser { Email = "e@x.com" });

        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);

        var svc = new AccountService(um.Object, emailSender.Object);
        var rs = await svc.RegisterAsync(new RegisterRequestDto { Email = "e@x.com" });

        LogResult(nameof(RegisterAsync_Returns_Error_When_Email_Already_Exists), rs);

        rs.Success.Should().BeFalse();
        rs.Message.Should().Be("Email already registered.");

        emailSender.VerifyNoOtherCalls();
        um.Verify(m => m.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_Returns_Error_When_Create_Fails()
    {
        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByEmailAsync("new@x.com")).ReturnsAsync((AppUser)null!);
        um.Setup(m => m.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
          .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "boom" }));

        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);

        var svc = new AccountService(um.Object, emailSender.Object);
        var rs = await svc.RegisterAsync(new RegisterRequestDto { Email = "new@x.com" });

        LogResult(nameof(RegisterAsync_Returns_Error_When_Create_Fails), rs);

        rs.Success.Should().BeFalse();
        rs.Message.Should().Contain("User creation failed:");
        rs.Message.Should().Contain("boom");

        emailSender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegisterAsync_Succeeds_And_Sends_Welcome_Email_With_6Char_TemporaryPassword_And_EmailConfirmed()
    {
        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByEmailAsync("ok@x.com")).ReturnsAsync((AppUser)null!);

        um.Setup(m => m.CreateAsync(It.Is<AppUser>(u =>
                u.Email == "ok@x.com" &&
                u.UserName == "ok@x.com" &&
                u.EmailConfirmed == true),
            It.IsAny<string>()))
          .ReturnsAsync(IdentityResult.Success);

        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        emailSender.Setup(es => es.SendAsync(
                "ok@x.com",
                It.Is<string>(s => s.Contains("Welcome", StringComparison.OrdinalIgnoreCase)),
                It.Is<string>(b => b.Contains("ok@x.com"))))
            .Returns(Task.CompletedTask);

        var svc = new AccountService(um.Object, emailSender.Object);
        var rs = await svc.RegisterAsync(new RegisterRequestDto { Email = "ok@x.com" });

        LogResult(nameof(RegisterAsync_Succeeds_And_Sends_Welcome_Email_With_6Char_TemporaryPassword_And_EmailConfirmed), rs);

        rs.Success.Should().BeTrue();
        rs.Data.Should().NotBeNull();
        rs.Data!.Email.Should().Be("ok@x.com");
        rs.Data.TemporaryPassword.Should().NotBeNullOrEmpty();
        rs.Data.TemporaryPassword.Length.Should().Be(6);

        emailSender.Verify(es => es.SendAsync(
            "ok@x.com",
            It.Is<string>(s => s.Contains("Welcome", StringComparison.OrdinalIgnoreCase)),
            It.Is<string>(b => b.Contains("ok@x.com") && b.Contains(rs.Data.TemporaryPassword))),
            Times.Once);

        emailSender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendPasswordResetOtpAsync_Returns_Failure_When_User_NotFound()
    {
        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByEmailAsync("nf@x.com")).ReturnsAsync((AppUser)null!);

        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);

        var svc = new AccountService(um.Object, emailSender.Object);
        var rs = await svc.SendPasswordResetOtpAsync(new ForgotPasswordDto { Email = "nf@x.com" });

        LogResult(nameof(SendPasswordResetOtpAsync_Returns_Failure_When_User_NotFound), rs);

        rs.Success.Should().BeFalse();
        rs.Message.Should().Be("This email is not registered in our system.");

        emailSender.VerifyNoOtherCalls();
        um.Verify(m => m.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Fact]
    public async Task SendPasswordResetOtpAsync_Returns_Failure_When_Email_Not_Confirmed()
    {
        var user = new AppUser { Email = "u@x.com", IsActive = true };
        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByEmailAsync("u@x.com")).ReturnsAsync(user);
        um.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(false);

        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);

        var svc = new AccountService(um.Object, emailSender.Object);
        var rs = await svc.SendPasswordResetOtpAsync(new ForgotPasswordDto { Email = "u@x.com" });

        LogResult(nameof(SendPasswordResetOtpAsync_Returns_Failure_When_Email_Not_Confirmed), rs);

        rs.Success.Should().BeFalse();
        rs.Message.Should().Be("Your email address has not been verified. Please check your inbox.");

        emailSender.VerifyNoOtherCalls();
        um.Verify(m => m.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Fact]
    public async Task SendPasswordResetOtpAsync_Returns_Failure_When_User_Inactive()
    {
        var user = new AppUser { Email = "u@x.com", IsActive = false };
        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByEmailAsync("u@x.com")).ReturnsAsync(user);
        um.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(true);

        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);

        var svc = new AccountService(um.Object, emailSender.Object);
        var rs = await svc.SendPasswordResetOtpAsync(new ForgotPasswordDto { Email = "u@x.com" });

        LogResult(nameof(SendPasswordResetOtpAsync_Returns_Failure_When_User_Inactive), rs);

        rs.Success.Should().BeFalse();
        rs.Message.Should().Be("Your account has been deactivated. Please contact support.");

        emailSender.VerifyNoOtherCalls();
        um.Verify(m => m.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Fact]
    public async Task SendPasswordResetOtpAsync_Sets_Otp_Expiry_Updates_And_Sends_Email()
    {
        var user = new AppUser { Email = "u@x.com", IsActive = true };
        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByEmailAsync("u@x.com")).ReturnsAsync(user);
        um.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
        um.Setup(m => m.UpdateAsync(It.IsAny<AppUser>())).ReturnsAsync(IdentityResult.Success);

        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        emailSender.Setup(es => es.SendAsync(
                "u@x.com",
                It.Is<string>(s => s.Contains("Reset", StringComparison.OrdinalIgnoreCase)),
                It.Is<string>(b => b.Contains("2 minutes"))))
            .Returns(Task.CompletedTask);

        var svc = new AccountService(um.Object, emailSender.Object);

        var before = DateTime.UtcNow;
        var rs = await svc.SendPasswordResetOtpAsync(new ForgotPasswordDto { Email = "u@x.com" });
        var after = DateTime.UtcNow;

        LogResult(nameof(SendPasswordResetOtpAsync_Sets_Otp_Expiry_Updates_And_Sends_Email), new
        {
            rs,
            user.PasswordResetOtp,
            user.PasswordResetOtpExpiry
        });

        rs.Success.Should().BeTrue();
        rs.Message.Should().Be("An OTP has been sent to your email address.");

        user.PasswordResetOtp.Should().NotBeNullOrEmpty();
        user.PasswordResetOtp!.Length.Should().Be(6);
        user.PasswordResetOtp!.All(char.IsDigit).Should().BeTrue();

        user.PasswordResetOtpExpiry.Should().NotBeNull();
        user.PasswordResetOtpExpiry!.Value.Should().BeOnOrAfter(before.AddMinutes(1.9));
        user.PasswordResetOtpExpiry!.Value.Should().BeOnOrBefore(after.AddMinutes(2.1));

        um.Verify(m => m.UpdateAsync(It.Is<AppUser>(u =>
            u.PasswordResetOtp == user.PasswordResetOtp &&
            u.PasswordResetOtpExpiry == user.PasswordResetOtpExpiry)), Times.Once);

        emailSender.Verify(es => es.SendAsync(
            "u@x.com",
            It.Is<string>(s => s.Contains("Reset", StringComparison.OrdinalIgnoreCase)),
            It.Is<string>(b => b.Contains(user.PasswordResetOtp!) && b.Contains("2 minutes"))),
            Times.Once);

        emailSender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendResetPasswordEmailAsync_Sends_Email_With_Otp()
    {
        var um = CreateUserManagerMock();

        var emailSender = new Mock<IEmailSender>(MockBehavior.Strict);
        emailSender.Setup(es => es.SendAsync(
                "u@x.com",
                It.Is<string>(s => s.Contains("Reset", StringComparison.OrdinalIgnoreCase)),
                It.Is<string>(b => b.Contains("123456") && b.Contains("2 minutes"))))
            .Returns(Task.CompletedTask);

        var svc = new AccountService(um.Object, emailSender.Object);
        await svc.SendResetPasswordEmailAsync("u@x.com", "123456");

        emailSender.VerifyAll();
        emailSender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task VerifyOtpAsync_Returns_Fail_When_User_Not_Found()
    {
        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByEmailAsync("nf@x.com")).ReturnsAsync((AppUser)null!);

        var svc = new AccountService(um.Object, Mock.Of<IEmailSender>());

        var rs = await svc.VerifyOtpAsync("nf@x.com", "123456");

        LogResult(nameof(VerifyOtpAsync_Returns_Fail_When_User_Not_Found), rs);

        rs.Success.Should().BeFalse();
        rs.Message.Should().Be("User not found.");
    }

    [Fact]
    public async Task VerifyOtpAsync_Returns_Fail_When_Otp_Invalid()
    {
        var user = new AppUser { Email = "u@x.com", PasswordResetOtp = "654321", PasswordResetOtpExpiry = DateTime.UtcNow.AddMinutes(1) };
        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByEmailAsync("u@x.com")).ReturnsAsync(user);

        var svc = new AccountService(um.Object, Mock.Of<IEmailSender>());
        var rs = await svc.VerifyOtpAsync("u@x.com", "000000");

        LogResult(nameof(VerifyOtpAsync_Returns_Fail_When_Otp_Invalid), rs);

        rs.Success.Should().BeFalse();
        rs.Message.Should().Be("Invalid OTP.");
    }

    [Fact]
    public async Task VerifyOtpAsync_Returns_Fail_When_Otp_Expired()
    {
        var user = new AppUser { Email = "u@x.com", PasswordResetOtp = "111111", PasswordResetOtpExpiry = DateTime.UtcNow.AddSeconds(-1) };
        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByEmailAsync("u@x.com")).ReturnsAsync(user);

        var svc = new AccountService(um.Object, Mock.Of<IEmailSender>());
        var rs = await svc.VerifyOtpAsync("u@x.com", "111111");

        LogResult(nameof(VerifyOtpAsync_Returns_Fail_When_Otp_Expired), rs);

        rs.Success.Should().BeFalse();
        rs.Message.Should().Be("OTP has expired.");
    }

    [Fact]
    public async Task VerifyOtpAsync_Succeeds_When_Otp_Valid()
    {
        var user = new AppUser { Email = "u@x.com", PasswordResetOtp = "111111", PasswordResetOtpExpiry = DateTime.UtcNow.AddMinutes(1) };
        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByEmailAsync("u@x.com")).ReturnsAsync(user);

        var svc = new AccountService(um.Object, Mock.Of<IEmailSender>());
        var rs = await svc.VerifyOtpAsync("u@x.com", "111111");

        LogResult(nameof(VerifyOtpAsync_Succeeds_When_Otp_Valid), rs);

        rs.Success.Should().BeTrue();
        rs.Data.Should().BeTrue();
        rs.Message.Should().Be("OTP verified successfully.");
    }

    [Fact]
    public async Task ResetPasswordWithOtpAsync_Returns_Fail_When_User_Not_Found()
    {
        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByEmailAsync("nf@x.com")).ReturnsAsync((AppUser)null!);

        var svc = new AccountService(um.Object, Mock.Of<IEmailSender>());

        var rs = await svc.ResetPasswordWithOtpAsync(new ResetPasswordWithOtpDto
        {
            Email = "nf@x.com",
            Otp = "111111",
            NewPassword = "Abc!2345"
        });

        LogResult(nameof(ResetPasswordWithOtpAsync_Returns_Fail_When_User_Not_Found), rs);

        rs.Success.Should().BeFalse();
        rs.Message.Should().Be("User not found.");
    }

    [Fact]
    public async Task ResetPasswordWithOtpAsync_Returns_Fail_When_Otp_Invalid()
    {
        var user = new AppUser
        {
            Email = "u@x.com",
            PasswordResetOtp = "654321",
            PasswordResetOtpExpiry = DateTime.UtcNow.AddMinutes(1)
        };

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
        rs.Message.Should().Be("Invalid OTP.");
    }

    [Fact]
    public async Task ResetPasswordWithOtpAsync_Returns_Fail_When_Otp_Expired()
    {
        var user = new AppUser
        {
            Email = "u@x.com",
            PasswordResetOtp = "111111",
            PasswordResetOtpExpiry = DateTime.UtcNow.AddSeconds(-1)
        };

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
        rs.Message.Should().Be("OTP has expired.");
    }

    [Fact]
    public async Task ResetPasswordWithOtpAsync_Returns_Fail_When_ResetPassword_Fails_And_DoesNot_Clear_Otp()
    {
        var user = new AppUser
        {
            Email = "u@x.com",
            PasswordResetOtp = "111111",
            PasswordResetOtpExpiry = DateTime.UtcNow.AddMinutes(1)
        };

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

        LogResult(nameof(ResetPasswordWithOtpAsync_Returns_Fail_When_ResetPassword_Fails_And_DoesNot_Clear_Otp), rs);

        rs.Success.Should().BeFalse();
        rs.Message.Should().Contain("Reset password failed:");
        rs.Message.Should().Contain("weak");

        user.PasswordResetOtp.Should().Be("111111");
        user.PasswordResetOtpExpiry.Should().NotBeNull();

        um.Verify(m => m.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordWithOtpAsync_Succeeds_Clears_Otp_And_Updates_User()
    {
        var user = new AppUser
        {
            Email = "u@x.com",
            PasswordResetOtp = "111111",
            PasswordResetOtpExpiry = DateTime.UtcNow.AddMinutes(1)
        };

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
        rs.Message.Should().Be("Password has been reset successfully.");

        user.PasswordResetOtp.Should().BeNull();
        user.PasswordResetOtpExpiry.Should().BeNull();

        um.Verify(m => m.UpdateAsync(It.Is<AppUser>(u =>
            u.PasswordResetOtp == null &&
            u.PasswordResetOtpExpiry == null)), Times.Once);
    }

    [Fact]
    public async Task GetAllSellersAsync_Returns_Mapped_Dtos()
    {
        var sellers = new List<AppUser>
        {
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

    [Fact]
    public async Task GetAllDesignersAsync_Returns_Mapped_Dtos()
    {
        var designers = new List<AppUser>
        {
            new AppUser { Id = "D1", FullName = "Designer One" },
            new AppUser { Id = "D2", FullName = "Designer Two" }
        };

        var um = CreateUserManagerMock();
        um.Setup(m => m.GetUsersInRoleAsync("Designer")).ReturnsAsync(designers);

        var svc = new AccountService(um.Object, Mock.Of<IEmailSender>());
        var rs = await svc.GetAllDesignersAsync();

        rs.Should().HaveCount(2);
        rs.Should().ContainSingle(d => d.DesignerId == "D1" && d.DesignerName == "Designer One");
        rs.Should().ContainSingle(d => d.DesignerId == "D2" && d.DesignerName == "Designer Two");
    }

    [Fact]
    public async Task UpdateProfileAsync_Returns_Fail_When_User_Not_Found()
    {
        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByIdAsync("U1")).ReturnsAsync((AppUser)null!);

        var svc = new AccountService(um.Object, Mock.Of<IEmailSender>());
        var rs = await svc.UpdateProfileAsync("U1", new UpdateProfileDto { FullName = "A", PhoneNumber = "1" });

        LogResult(nameof(UpdateProfileAsync_Returns_Fail_When_User_Not_Found), rs);

        rs.Success.Should().BeFalse();
        rs.Message.Should().Be("User not found.");
    }

    [Fact]
    public async Task UpdateProfileAsync_Returns_Fail_When_Update_Fails()
    {
        var user = new AppUser { Id = "U1", FullName = "Old", PhoneNumber = "0" };

        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByIdAsync("U1")).ReturnsAsync(user);
        um.Setup(m => m.UpdateAsync(It.IsAny<AppUser>()))
          .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "boom" }));

        var svc = new AccountService(um.Object, Mock.Of<IEmailSender>());
        var rs = await svc.UpdateProfileAsync("U1", new UpdateProfileDto { FullName = "New", PhoneNumber = "9" });

        LogResult(nameof(UpdateProfileAsync_Returns_Fail_When_Update_Fails), rs);

        rs.Success.Should().BeFalse();
        rs.Message.Should().Contain("Update failed:");
        rs.Message.Should().Contain("boom");
    }

    [Fact]
    public async Task UpdateProfileAsync_Succeeds_When_Update_Ok()
    {
        var user = new AppUser { Id = "U1", FullName = "Old", PhoneNumber = "0" };

        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByIdAsync("U1")).ReturnsAsync(user);
        um.Setup(m => m.UpdateAsync(It.IsAny<AppUser>())).ReturnsAsync(IdentityResult.Success);

        var svc = new AccountService(um.Object, Mock.Of<IEmailSender>());
        var rs = await svc.UpdateProfileAsync("U1", new UpdateProfileDto { FullName = "New", PhoneNumber = "9" });

        LogResult(nameof(UpdateProfileAsync_Succeeds_When_Update_Ok), rs);

        rs.Success.Should().BeTrue();
        rs.Data.Should().BeTrue();
        rs.Message.Should().Be("Profile updated successfully.");

        user.FullName.Should().Be("New");
        user.PhoneNumber.Should().Be("9");
    }
}
