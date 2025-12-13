using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CB_Gift.Tests.Services
{
    public class ManagementAccountServiceTests : IDisposable
    {
        private readonly CBGiftDbContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ManagementAccountService _svc;

        public ManagementAccountServiceTests()
        {
            var dbName = $"CBGiftTests_{Guid.NewGuid():N}";
            var options = new DbContextOptionsBuilder<CBGiftDbContext>()
                .UseInMemoryDatabase(dbName)
                .EnableSensitiveDataLogging()
                .Options;

            _db = new CBGiftDbContext(options);

            var userStore = new UserStore<AppUser, IdentityRole, CBGiftDbContext>(_db);
            var roleStore = new RoleStore<IdentityRole, CBGiftDbContext>(_db);

            var idOptions = new Microsoft.Extensions.Options.OptionsWrapper<IdentityOptions>(new IdentityOptions
            {
                Password = new PasswordOptions
                {
                    RequireDigit = false,
                    RequiredLength = 3,
                    RequireLowercase = false,
                    RequireNonAlphanumeric = false,
                    RequireUppercase = false
                },
                User = new UserOptions
                {
                    RequireUniqueEmail = true
                }
            });

            var userValidators = new List<IUserValidator<AppUser>> { new UserValidator<AppUser>() };
            var pwdValidators = new List<IPasswordValidator<AppUser>> { new PasswordValidator<AppUser>() };

            _userManager = new UserManager<AppUser>(
                userStore,
                idOptions,
                new PasswordHasher<AppUser>(),
                userValidators,
                pwdValidators,
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                new Microsoft.Extensions.Logging.LoggerFactory().CreateLogger<UserManager<AppUser>>()
            );

            _roleManager = new RoleManager<IdentityRole>(
                roleStore,
                new[] { new RoleValidator<IdentityRole>() },
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                new Microsoft.Extensions.Logging.LoggerFactory().CreateLogger<RoleManager<IdentityRole>>()
            );

            _svc = new ManagementAccountService(_userManager, _roleManager, _db);
        }

        public void Dispose()
        {
            _db?.Dispose();
        }

        // ===================== Helpers =====================

        private async Task<AppUser> SeedUserAsync(string email, string fullName, bool isActive = true, string password = "123")
        {
            var u = new AppUser
            {
                Email = email,
                UserName = email,
                FullName = fullName,
                IsActive = isActive,
                EmailConfirmed = true
            };

            var res = await _userManager.CreateAsync(u, password);
            Assert.True(res.Succeeded, string.Join(" | ", res.Errors.Select(e => e.Description)));
            return u;
        }

        private async Task EnsureRoleAsync(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var rr = await _roleManager.CreateAsync(new IdentityRole(roleName));
                Assert.True(rr.Succeeded, string.Join(" | ", rr.Errors.Select(e => e.Description)));
            }
        }

        private async Task AssignRoleAsync(AppUser u, params string[] roles)
        {
            foreach (var r in roles.Distinct(StringComparer.OrdinalIgnoreCase))
                await EnsureRoleAsync(r);

            var res = await _userManager.AddToRolesAsync(u, roles);
            Assert.True(res.Succeeded, string.Join(" | ", res.Errors.Select(e => e.Description)));
        }

        // ===================== Tests =====================

        [Fact]
        public async Task GetUsersAsync_Filters_Sorts_Pages_And_Excludes_ManagerRoleGlobally()
        {
            // Arrange
            var u1 = await SeedUserAsync("alice@x.com", "Alice", isActive: true);
            var u2 = await SeedUserAsync("bob@x.com", "Bob", isActive: false);
            var u3 = await SeedUserAsync("charlie@x.com", "Charlie", isActive: true);

            // user bị loại global vì có role Manager
            var u4 = await SeedUserAsync("manager@x.com", "Lina Manager", isActive: true);

            await AssignRoleAsync(u1, "Admin");
            await AssignRoleAsync(u2, "Staff");
            await AssignRoleAsync(u3, "Admin");
            await AssignRoleAsync(u4, "Admin", "Manager"); // có Manager => phải bị loại khỏi GetUsersAsync

            // Act 1: Search "li" (Alice, Charlie, Lina Manager), IsActive=true, Role=Admin
            // Nhưng Lina Manager phải bị loại => chỉ còn Alice + Charlie
            var q1 = new UserQuery
            {
                Search = "li",
                IsActive = true,
                Role = "Admin",
                SortBy = "email",
                SortDir = "asc",
                Page = 1,
                PageSize = 1
            };

            var r1 = await _svc.GetUsersAsync(q1);

            // Assert
            Assert.Equal(2, r1.TotalItems);           // Alice + Charlie (không tính Lina Manager)
            var p1 = Assert.Single(r1.Items);
            Assert.Equal("alice@x.com", p1.Email);
            Assert.Contains("Admin", p1.Roles);

            // Act 2: page 2
            q1.Page = 2;
            var r2 = await _svc.GetUsersAsync(q1);

            var p2 = Assert.Single(r2.Items);
            Assert.Equal("charlie@x.com", p2.Email);
            Assert.Contains("Admin", p2.Roles);

            // Act 3: Role filter Staff => Bob vẫn ra dù inactive (vì query không lọc IsActive nếu không truyền)
            var q2 = new UserQuery { Role = "Staff" };
            var r3 = await _svc.GetUsersAsync(q2);

            Assert.Equal(1, r3.TotalItems);
            var p3 = Assert.Single(r3.Items);
            Assert.Equal("bob@x.com", p3.Email);
            Assert.Contains("Staff", p3.Roles);
            Assert.False(p3.IsActive);

            // Act 4: Try filter Role=Manager => phải luôn = 0 vì bị loại global
            var q3 = new UserQuery { Role = "Manager" };
            var r4 = await _svc.GetUsersAsync(q3);
            Assert.Equal(0, r4.TotalItems);
            Assert.Empty(r4.Items);
        }

        [Fact]
        public async Task GetByIdAsync_Returns_Dto_With_Roles()
        {
            var u = await SeedUserAsync("john@x.com", "John");
            await AssignRoleAsync(u, "Admin", "Manager");

            // NOTE: GetByIdAsync dùng UserManager trực tiếp => vẫn thấy Manager role (không áp dụng rule loại bỏ)
            var dto = await _svc.GetByIdAsync(u.Id);
            Assert.NotNull(dto);
            Assert.Equal(u.Email, dto!.Email);
            Assert.Equal(u.FullName, dto.FullName);
            Assert.Contains("Admin", dto.Roles);
            Assert.Contains("Manager", dto.Roles);
        }

        [Fact]
        public async Task CreateAsync_Creates_New_User_And_Roles()
        {
            var dto = new CreateUserDto
            {
                Email = "new@x.com",
                FullName = "New Guy",
                IsActive = true,
                Password = "abc",
                Roles = new List<string> { "C", "D" }
            };

            var res = await _svc.CreateAsync(dto);

            Assert.True(res.Success, res.Message);
            Assert.NotNull(res.Data);
            Assert.Equal("new@x.com", res.Data!.Email);
            Assert.Contains("C", res.Data.Roles);
            Assert.Contains("D", res.Data.Roles);

            var u = await _userManager.FindByEmailAsync("new@x.com");
            Assert.NotNull(u);

            var roles = await _userManager.GetRolesAsync(u!);
            Assert.Contains("C", roles);
            Assert.Contains("D", roles);
        }

        [Fact]
        public async Task CreateAsync_If_Exists_Update_Profile_And_Roles()
        {
            // Arrange
            var u = await SeedUserAsync("exist@x.com", "Old Name", isActive: false);
            await AssignRoleAsync(u, "Staff");

            // Act
            var dto = new CreateUserDto
            {
                Email = "exist@x.com",
                FullName = "New Name",
                IsActive = true,
                Password = "ignored-when-existing",
                Roles = new List<string> { "Admin", "Manager" }
            };

            var res = await _svc.CreateAsync(dto);

            // Assert
            Assert.True(res.Success, res.Message);
            Assert.Equal("Existing user updated successfully.", res.Message);
            Assert.Equal("New Name", res.Data!.FullName);
            Assert.True(res.Data.IsActive);

            Assert.DoesNotContain("Staff", res.Data.Roles);
            Assert.Contains("Admin", res.Data.Roles);
            Assert.Contains("Manager", res.Data.Roles);
        }

        [Fact]
        public async Task UpdateAsync_Updates_Basic_Fields_And_Normalizes()
        {
            var u = await SeedUserAsync("old@x.com", "Old", isActive: true);

            var res = await _svc.UpdateAsync(new UpdateUserDto
            {
                Id = u.Id,
                FullName = "New Full",
                Email = "NEW@x.com",
                IsActive = false
            });

            Assert.True(res.Success, res.Message);
            Assert.Equal("Updated.", res.Message);

            var after = await _userManager.FindByIdAsync(u.Id);
            Assert.NotNull(after);

            Assert.Equal("New Full", after!.FullName);
            Assert.Equal("NEW@x.com", after.Email);
            Assert.Equal("NEW@x.com", after.UserName);
            Assert.Equal("NEW@X.COM", after.NormalizedEmail);
            Assert.Equal("NEW@X.COM", after.NormalizedUserName);
            Assert.False(after.IsActive);
        }

        [Fact]
        public async Task SetRolesAsync_Replaces_Roles()
        {
            var u = await SeedUserAsync("r@x.com", "Role User");
            await AssignRoleAsync(u, "A", "B");

            var res = await _svc.SetRolesAsync(new SetRolesDto
            {
                UserId = u.Id,
                Roles = new List<string> { "C", "D" }
            });

            Assert.True(res.Success, res.Message);
            Assert.Equal("Roles updated.", res.Message);

            var roles = await _userManager.GetRolesAsync(u);
            Assert.DoesNotContain("A", roles);
            Assert.DoesNotContain("B", roles);
            Assert.Contains("C", roles);
            Assert.Contains("D", roles);
        }

        //[Fact]
        //public async Task AdminResetPasswordAsync_Works()
        //{
        //    var u = await SeedUserAsync("p@x.com", "Pwd", password: "OldPwd@123");

        //    var res = await _svc.AdminResetPasswordAsync(new AdminResetPasswordDto
        //    {
        //        UserId = u.Id,
        //        NewPassword = "NewPwd@123"
        //    });

        //    Assert.True(res.Success, res.Message);
        //    Assert.Equal("Password reset.", res.Message);

        //    var reloaded = await _userManager.FindByIdAsync(u.Id);
        //    Assert.NotNull(reloaded);

        //    var ok = await _userManager.CheckPasswordAsync(reloaded!, "NewPwd@123");
        //    Assert.True(ok);
        //}

        [Fact]
        public async Task ToggleLockAsync_Lock_Then_Unlock()
        {
            var u = await SeedUserAsync("lock@x.com", "L");

            // Lock
            var r1 = await _svc.ToggleLockAsync(new ToggleLockDto
            {
                UserId = u.Id,
                Lock = true,
                Minutes = 5
            });

            Assert.True(r1.Success, r1.Message);
            Assert.Equal("User locked.", r1.Message);

            var afterLock = await _userManager.FindByIdAsync(u.Id);
            Assert.NotNull(afterLock!.LockoutEnd);
            Assert.True(afterLock.LockoutEnd!.Value > DateTimeOffset.UtcNow);

            // Unlock
            var r2 = await _svc.ToggleLockAsync(new ToggleLockDto
            {
                UserId = u.Id,
                Lock = false
            });

            Assert.True(r2.Success, r2.Message);
            Assert.Equal("User unlocked.", r2.Message);

            var afterUnlock = await _userManager.FindByIdAsync(u.Id);
            Assert.Null(afterUnlock!.LockoutEnd);
        }

        [Fact]
        public async Task DeleteAsync_Sets_IsActiveFalse_And_Is_Idempotent()
        {
            var u = await SeedUserAsync("del@x.com", "DelUser", isActive: true);

            var r1 = await _svc.DeleteAsync(u.Id);
            Assert.True(r1.Success, r1.Message);
            Assert.Equal("User has been deactivated (IsActive = false).", r1.Message);
            Assert.True(r1.Data);

            var after = await _userManager.FindByIdAsync(u.Id);
            Assert.NotNull(after);
            Assert.False(after!.IsActive);

            // Call again => idempotent
            var r2 = await _svc.DeleteAsync(u.Id);
            Assert.True(r2.Success, r2.Message);
            Assert.True(r2.Data);
            Assert.Equal("User already inactive.", r2.Message);
        }
    }

    // ====== Support: Normalizer đơn giản giống Identity mặc định ======
    internal sealed class UpperInvariantLookupNormalizer : ILookupNormalizer
    {
        public string? NormalizeEmail(string? email) => email?.ToUpperInvariant();
        public string? NormalizeName(string? name) => name?.ToUpperInvariant();
    }
}
