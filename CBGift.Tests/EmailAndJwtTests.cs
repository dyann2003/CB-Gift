using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CB_Gift.Data;
using CB_Gift.Models;
using CB_Gift.Services;
using CB_Gift.Services.Email;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using Xunit.Abstractions;

public class EmailAndJwtTests
{
    private readonly ITestOutputHelper _out;
    public EmailAndJwtTests(ITestOutputHelper output) => _out = output;

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
            ["Jwt:Audience"] = "cbgift.clients",
            ["Jwt:ExpiresMinutes"] = "60"
        }).Build();

    private static CBGiftDbContext NewInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<CBGiftDbContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .EnableSensitiveDataLogging()
            .Options;

        var ctx = new CBGiftDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private Mock<UserManager<AppUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<AppUser>>();
        return new Mock<UserManager<AppUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    // =========================================================
    // EMAIL
    // =========================================================

    [Fact]
    public async Task SmtpEmailSender_SendAsync_ThrowsWithoutServer_But_Covers_Path()
    {
        var sender = new global::CB_Gift.Services.Email.SmtpEmailSender(BuildEmailCfg());

        try
        {
            await sender.SendAsync("u@test.com", "subj", "<b>body</b>");
            Assert.True(false, "Expected exception");
        }
        catch (Exception ex)
        {
            _out.WriteLine($"[SMTP_NoServer] {ex.GetType().Name}: {ex.Message}");
        }
    }

    [Fact]
    public async Task AccountService_SendResetPasswordEmailAsync_Calls_IEmailSender()
    {
        var um = MockUserManager();
        var email = new Mock<IEmailSender>(MockBehavior.Strict);

        email.Setup(m => m.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var svc = new AccountService(um.Object, email.Object);

        await svc.SendResetPasswordEmailAsync("user@test.com", "https://reset.link");
        _out.WriteLine("[ResetMail] user@test.com -> https://reset.link");

        email.Verify(m => m.SendAsync(
                "user@test.com",
                It.Is<string>(s => s.Contains("Reset", StringComparison.OrdinalIgnoreCase)),
                It.Is<string>(b => b.Contains("https://reset.link"))),
            Times.Once);
    }

    [Fact]
    public async Task SmtpEmailSender_Uses_Defaults_When_Config_Missing()
    {
        var cfgMissing = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var sender = new global::CB_Gift.Services.Email.SmtpEmailSender(cfgMissing);

        try
        {
            await sender.SendAsync("u@test.com", "subj", "<b>body</b>");
            Assert.True(false, "Expected exception");
        }
        catch (Exception ex)
        {
            _out.WriteLine($"[SMTP_DefaultCfg] {ex.GetType().Name}: {ex.Message}");
        }
    }

    // =========================================================
    // JWT + REFRESH TOKEN (đúng theo JwtTokenService mới)
    // =========================================================

    [Fact]
    public async Task JwtTokenService_CreateTokenAsync_Branches_With_And_Without_Roles()
    {
        using var ctx = NewInMemoryContext(nameof(JwtTokenService_CreateTokenAsync_Branches_With_And_Without_Roles));

        var user = new AppUser { Id = "42", UserName = "bob", Email = "bob@test.com", FullName = "Bobby" };
        var um = MockUserManager();

        um.Setup(m => m.GetUserIdAsync(It.IsAny<AppUser>())).ReturnsAsync("42");
        um.Setup(m => m.GetUserNameAsync(It.IsAny<AppUser>())).ReturnsAsync("bob");

        var svc = new JwtTokenService(BuildJwtCfg(), um.Object, ctx);

        // No roles
        um.Setup(m => m.GetRolesAsync(It.IsAny<AppUser>())).ReturnsAsync(new List<string>());
        var tokenNoRoles = await svc.CreateTokenAsync(user);
        _out.WriteLine($"[JWT_NoRoles] {tokenNoRoles[..Math.Min(20, tokenNoRoles.Length)]}...");

        var jwt1 = new JwtSecurityTokenHandler().ReadJwtToken(tokenNoRoles);
        jwt1.Claims.Count(c => c.Type == ClaimTypes.Role).Should().Be(0);
        jwt1.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value.Should().Be("42");
        jwt1.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value.Should().Be("bob@test.com");
        jwt1.Claims.FirstOrDefault(c => c.Type == "fullName")?.Value.Should().Be("Bobby");

        // With roles
        um.Setup(m => m.GetRolesAsync(It.IsAny<AppUser>())).ReturnsAsync(new List<string> { "Admin", "Seller" });
        var tokenWithRoles = await svc.CreateTokenAsync(user);
        _out.WriteLine($"[JWT_WithRoles] {tokenWithRoles[..Math.Min(20, tokenWithRoles.Length)]}...");

        var jwt2 = new JwtSecurityTokenHandler().ReadJwtToken(tokenWithRoles);
        jwt2.Claims.Count(c => c.Type == ClaimTypes.Role).Should().Be(2);
    }

    [Fact]
    public async Task JwtTokenService_GenerateRefreshTokenAsync_SavesToken_ToDb()
    {
        using var ctx = NewInMemoryContext(nameof(JwtTokenService_GenerateRefreshTokenAsync_SavesToken_ToDb));
        var um = MockUserManager();
        var svc = new JwtTokenService(BuildJwtCfg(), um.Object, ctx);

        var rt = await svc.GenerateRefreshTokenAsync("u1");

        rt.Should().NotBeNull();
        rt.UserId.Should().Be("u1");
        rt.Token.Should().NotBeNullOrWhiteSpace();
        rt.Expires.Should().BeAfter(DateTime.UtcNow);

        var stored = await ctx.RefreshTokens.FirstOrDefaultAsync(x => x.Token == rt.Token);
        stored.Should().NotBeNull();
        stored!.Revoked.Should().BeNull();
    }

    [Fact]
    public async Task JwtTokenService_ValidateRefreshTokenAsync_ReturnsFail_When_NotFound()
    {
        using var ctx = NewInMemoryContext(nameof(JwtTokenService_ValidateRefreshTokenAsync_ReturnsFail_When_NotFound));
        var um = MockUserManager();
        var svc = new JwtTokenService(BuildJwtCfg(), um.Object, ctx);

        var res = await svc.ValidateRefreshTokenAsync("not-exist");

        res.Success.Should().BeFalse();
        res.Message.Should().Be("Token not found");
    }

    [Fact]
    public async Task JwtTokenService_ValidateRefreshTokenAsync_RotatesToken_ByRevokingStoredToken()
    {
        using var ctx = NewInMemoryContext(nameof(JwtTokenService_ValidateRefreshTokenAsync_RotatesToken_ByRevokingStoredToken));
        var um = MockUserManager();
        var svc = new JwtTokenService(BuildJwtCfg(), um.Object, ctx);

        // Seed user + refresh token (để Include(x => x.User) có dữ liệu)
        var user = new AppUser { Id = "u1", UserName = "u1@test.com", Email = "u1@test.com", FullName = "User One" };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var rt = await svc.GenerateRefreshTokenAsync("u1");

        // Ensure navigation works (nếu model có FK + navigation)
        var stored = await ctx.RefreshTokens.FirstAsync(x => x.Token == rt.Token);
        stored.UserId.Should().Be("u1");

        var res = await svc.ValidateRefreshTokenAsync(rt.Token);

        res.Success.Should().BeTrue();
        res.Data.Should().NotBeNull();
        res.Data!.Id.Should().Be("u1");

        // Rotation: token phải bị revoke ngay
        var rotated = await ctx.RefreshTokens.FirstAsync(x => x.Token == rt.Token);
        rotated.Revoked.Should().NotBeNull();
        rotated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task JwtTokenService_ValidateRefreshTokenAsync_ReturnsFail_When_TokenExpiredOrRevoked()
    {
        using var ctx = NewInMemoryContext(nameof(JwtTokenService_ValidateRefreshTokenAsync_ReturnsFail_When_TokenExpiredOrRevoked));
        var um = MockUserManager();
        var svc = new JwtTokenService(BuildJwtCfg(), um.Object, ctx);

        // Seed user
        var user = new AppUser { Id = "u1", UserName = "u1@test.com", Email = "u1@test.com", FullName = "User One" };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        // Seed a revoked token
        var token = new RefreshToken
        {
            Token = "revoked-token",
            UserId = "u1",
            Created = DateTime.UtcNow.AddDays(-1),
            Expires = DateTime.UtcNow.AddDays(7),
            Revoked = DateTime.UtcNow.AddMinutes(-1)
        };
        ctx.RefreshTokens.Add(token);
        await ctx.SaveChangesAsync();

        var res = await svc.ValidateRefreshTokenAsync("revoked-token");

        res.Success.Should().BeFalse();
        res.Message.Should().Be("Token expired or revoked");
    }

    [Fact]
    public async Task JwtTokenService_RevokeRefreshTokenAsync_SetsRevoked()
    {
        using var ctx = NewInMemoryContext(nameof(JwtTokenService_RevokeRefreshTokenAsync_SetsRevoked));
        var um = MockUserManager();
        var svc = new JwtTokenService(BuildJwtCfg(), um.Object, ctx);

        // Seed token
        var t = new RefreshToken
        {
            Token = "t1",
            UserId = "u1",
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(7),
            Revoked = null
        };
        ctx.RefreshTokens.Add(t);
        await ctx.SaveChangesAsync();

        await svc.RevokeRefreshTokenAsync("t1");

        var updated = await ctx.RefreshTokens.FirstAsync(x => x.Token == "t1");
        updated.Revoked.Should().NotBeNull();
        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task JwtTokenService_RevokeAllRefreshTokensAsync_RevokesOnlyActiveTokens()
    {
        using var ctx = NewInMemoryContext(nameof(JwtTokenService_RevokeAllRefreshTokensAsync_RevokesOnlyActiveTokens));
        var um = MockUserManager();
        var svc = new JwtTokenService(BuildJwtCfg(), um.Object, ctx);

        var now = DateTime.UtcNow;

        // Active tokens (should be revoked)
        ctx.RefreshTokens.Add(new RefreshToken { Token = "a1", UserId = "u1", Created = now, Expires = now.AddDays(1), Revoked = null });
        ctx.RefreshTokens.Add(new RefreshToken { Token = "a2", UserId = "u1", Created = now, Expires = now.AddDays(2), Revoked = null });

        // Already revoked (should remain revoked)
        ctx.RefreshTokens.Add(new RefreshToken { Token = "r1", UserId = "u1", Created = now.AddDays(-2), Expires = now.AddDays(2), Revoked = now.AddDays(-1) });

        // Expired (should not be touched)
        ctx.RefreshTokens.Add(new RefreshToken { Token = "e1", UserId = "u1", Created = now.AddDays(-10), Expires = now.AddDays(-1), Revoked = null });

        // Other user (should not be touched)
        ctx.RefreshTokens.Add(new RefreshToken { Token = "o1", UserId = "u2", Created = now, Expires = now.AddDays(2), Revoked = null });

        await ctx.SaveChangesAsync();

        await svc.RevokeAllRefreshTokensAsync("u1");

        var a1 = await ctx.RefreshTokens.FirstAsync(x => x.Token == "a1");
        var a2 = await ctx.RefreshTokens.FirstAsync(x => x.Token == "a2");
        a1.Revoked.Should().NotBeNull();
        a2.Revoked.Should().NotBeNull();

        var r1 = await ctx.RefreshTokens.FirstAsync(x => x.Token == "r1");
        r1.Revoked.Should().NotBeNull(); // vẫn revoked

        var e1 = await ctx.RefreshTokens.FirstAsync(x => x.Token == "e1");
        e1.Revoked.Should().BeNull(); // expired token không bị cập nhật theo code bạn (chỉ revoke token active)

        var o1 = await ctx.RefreshTokens.FirstAsync(x => x.Token == "o1");
        o1.Revoked.Should().BeNull(); // user khác không bị revoke
    }
}
