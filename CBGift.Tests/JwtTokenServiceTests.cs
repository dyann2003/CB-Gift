using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CB_Gift.Data;
using CB_Gift.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

public class JwtTokenServiceTests
{
    private IConfiguration BuildConfig()
    {
        var dict = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "THIS_IS_A_DEMO_KEY_32_CHARS_MIN__",
            ["Jwt:Issuer"] = "cbgift.local",
            ["Jwt:Audience"] = "cbgift.clients",
            ["Jwt:ExpiresMinutes"] = "10"
        };
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    [Fact]
    public async Task CreateTokenAsync_Produces_Valid_JWT_With_Claims_And_Roles()
    {
        // Arrange
        var user = new AppUser { Id = "123", UserName = "abc", Email = "a@b.com", FullName = "Tester" };

        // Tạo mock UserManager
        var store = new Mock<IUserStore<AppUser>>();
        var userMgrMock = new Mock<UserManager<AppUser>>(
            store.Object, null, null, null, null, null, null, null, null);

        userMgrMock.Setup(m => m.GetRolesAsync(It.IsAny<AppUser>()))
                   .ReturnsAsync(new List<string> { "Manager", "Seller" });

        userMgrMock.Setup(m => m.GetUserIdAsync(It.IsAny<AppUser>()))
                   .ReturnsAsync("123");

        userMgrMock.Setup(m => m.GetUserNameAsync(It.IsAny<AppUser>()))
                   .ReturnsAsync("abc");

        var svc = new JwtTokenService(BuildConfig(), userMgrMock.Object);

    
        var token = await svc.CreateTokenAsync(user);

        // Assert
        token.Should().NotBeNullOrEmpty();

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        jwt.Issuer.Should().Be("cbgift.local");
        jwt.Audiences.Should().Contain("cbgift.clients");
        jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value.Should().Be("123");
        jwt.Claims.First(c => c.Type == "fullName").Value.Should().Be("Tester");
        jwt.Claims.Count(c => c.Type == ClaimTypes.Role).Should().Be(2);
        jwt.ValidTo.Should().BeAfter(DateTime.UtcNow);
    }
    [Fact]
    public async Task CreateTokenAsync_NoRoles_Uses_Default_Expiry_And_No_Role_Claims()
    {
        var user = new AppUser { Id = "U1", UserName = "norole", Email = "n@x.com", FullName = "No Role" };

        // config KHÔNG set Jwt:ExpiresMinutes -> rẽ nhánh mặc định "60"
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "THIS_IS_A_DEMO_KEY_32_CHARS_MIN__",
            ["Jwt:Issuer"] = "cbgift.local",
            ["Jwt:Audience"] = "cbgift.clients"
        }).Build();

        // Mock UserManager và trả về danh sách role RỖNG
        var store = new Mock<IUserStore<AppUser>>();
        var um = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
        um.Setup(m => m.GetRolesAsync(It.IsAny<AppUser>())).ReturnsAsync(new List<string>());
        um.Setup(m => m.GetUserIdAsync(It.IsAny<AppUser>())).ReturnsAsync("U1");
        um.Setup(m => m.GetUserNameAsync(It.IsAny<AppUser>())).ReturnsAsync("norole");

        var svc = new JwtTokenService(cfg, um.Object);
        var token = await svc.CreateTokenAsync(user);

        token.Should().NotBeNullOrEmpty();

        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Audiences.Should().Contain("cbgift.clients");
        // Không có claim Role
        jwt.Claims.Count(c => c.Type == System.Security.Claims.ClaimTypes.Role).Should().Be(0);
        // Expiry > now (mặc định 60 phút)
        jwt.ValidTo.Should().BeAfter(DateTime.UtcNow);
    }

}
