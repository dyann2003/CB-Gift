
using System.IdentityModel.Tokens.Jwt;

using CB_Gift.Data;
using CB_Gift.Services;
    
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using CB_Gift.Services.Email;

public class EmailAndJwtTests
{

    private IConfiguration BuildEmailCfg() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Email:SmtpServer"] = "localhost",   
            ["Email:SmtpPort"] = "25",
            ["Email:Sender"] = "noreply@test.local",
            ["Email:Password"] = "pwd"
        }).Build();

    private IConfiguration BuildJwtCfg() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "THIS_IS_A_DEMO_KEY_32_CHARS_MIN__",
            ["Jwt:Issuer"] = "cbgift.local",
            ["Jwt:Audience"] = "cbgift.clients"
           
        }).Build();

    // Mock UserManager<AppUser>
    private Mock<UserManager<AppUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<AppUser>>();
        return new Mock<UserManager<AppUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public async Task SmtpEmailSender_SendAsync_ThrowsWithoutServer_But_Covers_Path()
    {
        var sender = new global::CB_Gift.Services.Email.SmtpEmailSender(BuildEmailCfg());


        Func<Task> act = async () => await sender.SendAsync("u@test.com", "subj", "<b>body</b>");
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task AccountService_SendResetPasswordEmailAsync_Calls_IEmailSender()
    {
        var um = MockUserManager();
        var email = new Mock<IEmailSender>();
        var svc = new AccountService(um.Object, email.Object);   // AccountService(refactor) nhận 2 tham số

        await svc.SendResetPasswordEmailAsync("user@test.com", "https://reset.link");

        email.Verify(m => m.SendAsync(
                "user@test.com",
                It.Is<string>(s => s.Contains("Reset", StringComparison.OrdinalIgnoreCase)),
                It.Is<string>(b => b.Contains("https://reset.link"))),
            Times.Once);
    }
    [Fact]
    public async Task SmtpEmailSender_Uses_Defaults_When_Config_Missing()
    {
      
        var cfgMissing = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())  // trống
            .Build();

        var sender = new global::CB_Gift.Services.Email.SmtpEmailSender(cfgMissing);


        Func<Task> act = async () => await sender.SendAsync("u@test.com", "subj", "<b>body</b>");
        await act.Should().ThrowAsync<Exception>();
    }


    [Fact]
    public async Task JwtTokenService_CreateTokenAsync_Branches_With_And_Without_Roles()
    {
        var user = new AppUser { Id = "42", UserName = "bob", Email = "bob@test.com", FullName = "Bobby" };

        var um = MockUserManager();
        um.Setup(m => m.GetUserIdAsync(It.IsAny<AppUser>())).ReturnsAsync("42");
        um.Setup(m => m.GetUserNameAsync(It.IsAny<AppUser>())).ReturnsAsync("bob");

        var svc = new JwtTokenService(BuildJwtCfg(), um.Object);

        // Nhánh: không có roles
        um.Setup(m => m.GetRolesAsync(It.IsAny<AppUser>())).ReturnsAsync(new List<string>());
        var tokenNoRoles = await svc.CreateTokenAsync(user);
        var jwt1 = new JwtSecurityTokenHandler().ReadJwtToken(tokenNoRoles);
        jwt1.Claims.Count(c => c.Type == System.Security.Claims.ClaimTypes.Role).Should().Be(0);

        // Nhánh: có roles
        um.Setup(m => m.GetRolesAsync(It.IsAny<AppUser>())).ReturnsAsync(new List<string> { "Admin", "Seller" });
        var tokenWithRoles = await svc.CreateTokenAsync(user);
        var jwt2 = new JwtSecurityTokenHandler().ReadJwtToken(tokenWithRoles);
        jwt2.Claims.Count(c => c.Type == System.Security.Claims.ClaimTypes.Role).Should().Be(2);
    }
}
