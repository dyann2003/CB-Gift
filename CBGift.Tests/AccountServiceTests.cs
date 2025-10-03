using System.Threading.Tasks;
using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Services;
using CB_Gift.Services.Email;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

public class AccountServiceTests
{
    private static Mock<UserManager<AppUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<AppUser>>();
        return new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
    }

    [Fact]
    public async Task RegisterAsync_Returns_Error_When_Email_Already_Exists()
    {
        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByEmailAsync("e@x.com")).ReturnsAsync(new AppUser { Email = "e@x.com" });

        var emailSender = new Mock<IEmailSender>();
        var svc = new AccountService(um.Object, emailSender.Object);

        var rs = await svc.RegisterAsync(new RegisterRequestDto { Email = "e@x.com" });
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
        rs.Success.Should().BeFalse();
        rs.Message.Should().Contain("User creation failed");
        emailSender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegisterAsync_Succeeds_And_Sends_Welcome_Email()
    {
        var um = CreateUserManagerMock();
        um.Setup(m => m.FindByEmailAsync("ok@x.com")).ReturnsAsync((AppUser)null!);
        um.Setup(m => m.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
          .ReturnsAsync(IdentityResult.Success);

        var emailSender = new Mock<IEmailSender>();
        var svc = new AccountService(um.Object, emailSender.Object);

        var rs = await svc.RegisterAsync(new RegisterRequestDto { Email = "ok@x.com" });

        rs.Success.Should().BeTrue();
        rs.Data.Email.Should().Be("ok@x.com");
        rs.Data.TemporaryPassword.Should().NotBeNullOrEmpty();
        rs.Data.TemporaryPassword.Length.Should().Be(6);

        emailSender.Verify(es => es.SendAsync(
            "ok@x.com",
            It.Is<string>(s => s.Contains("Welcome")),
            It.Is<string>(b => b.Contains("ok@x.com") && b.Contains(rs.Data.TemporaryPassword))),
            Times.Once);
    }
    [Fact]
    public async Task SendResetPasswordEmailAsync_Sends_Email_With_Reset_Link()
    {
        var um = CreateUserManagerMock();
        var emailSender = new Mock<IEmailSender>();
        var svc = new AccountService(um.Object, emailSender.Object);

        await svc.SendResetPasswordEmailAsync("u@x.com", "https://x/reset?token=abc");

        emailSender.Verify(es => es.SendAsync(
            "u@x.com",
            It.Is<string>(s => s.Contains("Reset", StringComparison.OrdinalIgnoreCase)),
            It.Is<string>(b => b.Contains("https://x/reset?token=abc"))),
            Times.Once);
    }
}
