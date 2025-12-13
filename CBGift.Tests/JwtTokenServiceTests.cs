using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using CB_Gift.Data;
using CB_Gift.Models;
using CB_Gift.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace CB_Gift.Tests.Services
{
    public class JwtTokenServiceTests
    {
        // =========================
        // Helpers
        // =========================

        private static IConfiguration BuildJwtCfg() =>
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
                .EnableSensitiveDataLogging()
                .Options;

            var ctx = new CBGiftDbContext(options);
            ctx.Database.EnsureCreated();
            return ctx;
        }

        private static Mock<UserManager<AppUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<AppUser>>();
            return new Mock<UserManager<AppUser>>(
                store.Object,
                null!, null!, null!, null!, null!, null!, null!, null!
            );
        }

        // =========================
        // Tests
        // =========================

        [Fact]
        public async Task CreateTokenAsync_Branches_With_And_Without_Roles()
        {
            using var ctx = NewInMemoryContext(nameof(CreateTokenAsync_Branches_With_And_Without_Roles));

            var user = new AppUser
            {
                Id = "42",
                UserName = "bob",
                Email = "bob@test.com",
                FullName = "Bobby"
            };

            var um = MockUserManager();
            var svc = new JwtTokenService(BuildJwtCfg(), um.Object, ctx);

            // ----- Case 1: No roles -----
            um.Setup(m => m.GetRolesAsync(It.IsAny<AppUser>()))
              .ReturnsAsync(new List<string>());

            var tokenNoRoles = await svc.CreateTokenAsync(user);

            tokenNoRoles.Should().NotBeNullOrWhiteSpace();
            var jwt1 = new JwtSecurityTokenHandler().ReadJwtToken(tokenNoRoles);

            jwt1.Claims.Count(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Should().Be(0);

            jwt1.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value.Should().Be("42");
            jwt1.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value.Should().Be("bob@test.com");
            jwt1.Claims.Single(c => c.Type == "fullName").Value.Should().Be("Bobby");

            // ----- Case 2: With roles -----
            um.Setup(m => m.GetRolesAsync(It.IsAny<AppUser>()))
              .ReturnsAsync(new List<string> { "Admin", "Seller" });

            var tokenWithRoles = await svc.CreateTokenAsync(user);

            tokenWithRoles.Should().NotBeNullOrWhiteSpace();
            var jwt2 = new JwtSecurityTokenHandler().ReadJwtToken(tokenWithRoles);

            jwt2.Claims.Count(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Should().Be(2);

            jwt2.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .Should().BeEquivalentTo(new[] { "Admin", "Seller" });
        }

        [Fact]
        public async Task GenerateRefreshTokenAsync_Saves_To_Db()
        {
            using var ctx = NewInMemoryContext(nameof(GenerateRefreshTokenAsync_Saves_To_Db));

            var um = MockUserManager();
            var svc = new JwtTokenService(BuildJwtCfg(), um.Object, ctx);

            var rt = await svc.GenerateRefreshTokenAsync("user-1");

            rt.Should().NotBeNull();
            rt.UserId.Should().Be("user-1");
            rt.Token.Should().NotBeNullOrWhiteSpace();
            rt.Expires.Should().BeAfter(DateTime.UtcNow);

            var inDb = await ctx.RefreshTokens.FirstOrDefaultAsync(x => x.Token == rt.Token);
            inDb.Should().NotBeNull();
            inDb!.Revoked.Should().BeNull();
        }

        [Fact]
        public async Task ValidateRefreshTokenAsync_Returns_Fail_When_NotFound()
        {
            using var ctx = NewInMemoryContext(nameof(ValidateRefreshTokenAsync_Returns_Fail_When_NotFound));

            var um = MockUserManager();
            var svc = new JwtTokenService(BuildJwtCfg(), um.Object, ctx);

            var res = await svc.ValidateRefreshTokenAsync("not-exist");

            res.Success.Should().BeFalse();
            res.Message.Should().Contain("Token not found");
        }

        [Fact]
        public async Task ValidateRefreshTokenAsync_Success_And_Rotates_Revoke_Token()
        {
            using var ctx = NewInMemoryContext(nameof(ValidateRefreshTokenAsync_Success_And_Rotates_Revoke_Token));

            // Seed user + refresh token
            var user = new AppUser { Id = "u1", UserName = "u1", Email = "u1@test.com", FullName = "U1" };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            var refresh = new RefreshToken
            {
                Token = "token-abc",
                UserId = "u1",
                User = user,
                Created = DateTime.UtcNow.AddMinutes(-1),
                Expires = DateTime.UtcNow.AddDays(7),
                Revoked = null
            };
            ctx.RefreshTokens.Add(refresh);
            await ctx.SaveChangesAsync();

            var um = MockUserManager();
            var svc = new JwtTokenService(BuildJwtCfg(), um.Object, ctx);

            var res = await svc.ValidateRefreshTokenAsync("token-abc");

            res.Success.Should().BeTrue();
            res.Data.Should().NotBeNull();
            res.Data!.Id.Should().Be("u1");

            // Token must be revoked after validation (rotation)
            var after = await ctx.RefreshTokens.FirstAsync(x => x.Token == "token-abc");
            after.Revoked.Should().NotBeNull();
        }

        [Fact]
        public async Task ValidateRefreshTokenAsync_Fails_When_Expired_Or_Revoked()
        {
            using var ctx = NewInMemoryContext(nameof(ValidateRefreshTokenAsync_Fails_When_Expired_Or_Revoked));

            var user = new AppUser { Id = "u1", UserName = "u1", Email = "u1@test.com", FullName = "U1" };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            // Expired token (Expires < now)
            ctx.RefreshTokens.Add(new RefreshToken
            {
                Token = "expired",
                UserId = "u1",
                User = user,
                Created = DateTime.UtcNow.AddDays(-10),
                Expires = DateTime.UtcNow.AddDays(-1),
                Revoked = null
            });

            // Revoked token
            ctx.RefreshTokens.Add(new RefreshToken
            {
                Token = "revoked",
                UserId = "u1",
                User = user,
                Created = DateTime.UtcNow.AddDays(-1),
                Expires = DateTime.UtcNow.AddDays(7),
                Revoked = DateTime.UtcNow.AddMinutes(-5)
            });

            await ctx.SaveChangesAsync();

            var um = MockUserManager();
            var svc = new JwtTokenService(BuildJwtCfg(), um.Object, ctx);

            var r1 = await svc.ValidateRefreshTokenAsync("expired");
            r1.Success.Should().BeFalse();
            r1.Message.Should().Contain("expired or revoked");

            var r2 = await svc.ValidateRefreshTokenAsync("revoked");
            r2.Success.Should().BeFalse();
            r2.Message.Should().Contain("expired or revoked");
        }

        [Fact]
        public async Task RevokeRefreshTokenAsync_Sets_Revoked()
        {
            using var ctx = NewInMemoryContext(nameof(RevokeRefreshTokenAsync_Sets_Revoked));

            ctx.RefreshTokens.Add(new RefreshToken
            {
                Token = "t1",
                UserId = "u1",
                Created = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(7),
                Revoked = null
            });
            await ctx.SaveChangesAsync();

            var um = MockUserManager();
            var svc = new JwtTokenService(BuildJwtCfg(), um.Object, ctx);

            await svc.RevokeRefreshTokenAsync("t1");

            var after = await ctx.RefreshTokens.FirstAsync(x => x.Token == "t1");
            after.Revoked.Should().NotBeNull();
        }

        [Fact]
        public async Task RevokeAllRefreshTokensAsync_Revokes_All_Active_Tokens()
        {
            using var ctx = NewInMemoryContext(nameof(RevokeAllRefreshTokensAsync_Revokes_All_Active_Tokens));

            ctx.RefreshTokens.AddRange(
                new RefreshToken
                {
                    Token = "active1",
                    UserId = "u1",
                    Created = DateTime.UtcNow,
                    Expires = DateTime.UtcNow.AddDays(7),
                    Revoked = null
                },
                new RefreshToken
                {
                    Token = "active2",
                    UserId = "u1",
                    Created = DateTime.UtcNow,
                    Expires = DateTime.UtcNow.AddDays(7),
                    Revoked = null
                },
                new RefreshToken
                {
                    Token = "otherUser",
                    UserId = "u2",
                    Created = DateTime.UtcNow,
                    Expires = DateTime.UtcNow.AddDays(7),
                    Revoked = null
                },
                new RefreshToken
                {
                    Token = "alreadyRevoked",
                    UserId = "u1",
                    Created = DateTime.UtcNow,
                    Expires = DateTime.UtcNow.AddDays(7),
                    Revoked = DateTime.UtcNow.AddMinutes(-1)
                },
                new RefreshToken
                {
                    Token = "expired",
                    UserId = "u1",
                    Created = DateTime.UtcNow.AddDays(-10),
                    Expires = DateTime.UtcNow.AddDays(-1),
                    Revoked = null
                }
            );
            await ctx.SaveChangesAsync();

            var um = MockUserManager();
            var svc = new JwtTokenService(BuildJwtCfg(), um.Object, ctx);

            await svc.RevokeAllRefreshTokensAsync("u1");

            var u1Tokens = await ctx.RefreshTokens.Where(x => x.UserId == "u1").ToListAsync();

            u1Tokens.Single(x => x.Token == "active1").Revoked.Should().NotBeNull();
            u1Tokens.Single(x => x.Token == "active2").Revoked.Should().NotBeNull();

            // không đụng token của user khác
            u1Tokens.Any(x => x.Token == "otherUser").Should().BeFalse();

            // token đã revoke giữ nguyên
            u1Tokens.Single(x => x.Token == "alreadyRevoked").Revoked.Should().NotBeNull();

            // token expired không bắt buộc revoke theo logic hiện tại (chỉ revoke token còn hạn)
            u1Tokens.Single(x => x.Token == "expired").Revoked.Should().BeNull();
        }
    }
}
