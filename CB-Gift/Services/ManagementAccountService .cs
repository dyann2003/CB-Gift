// File: CB_Gift/Services/ManagementAccountService.cs
using CB_Gift.DTOs;
using CB_Gift.Data;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CB_Gift.Services;

public class ManagementAccountService : IManagementAccountService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    private readonly CBGiftDbContext _db;
    public ManagementAccountService(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        CBGiftDbContext db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
    }


    public async Task<PagedResult<UserSummaryDto>> GetUsersAsync(UserQuery q)
    {
        // 1) Base query
        var users = _db.Users.AsNoTracking().AsQueryable();

        // =========================================================================
        // [FIX] LOẠI BỎ ROLE "Manager" KHỎI DANH SÁCH
        // Logic: Lọc những user MÀ KHÔNG CÓ (Any) bản ghi UserRole nào trỏ tới Role "Manager"
        // =========================================================================
        users = from u in users
                where !(from ur in _db.UserRoles
                        join r in _db.Roles on ur.RoleId equals r.Id
                        where ur.UserId == u.Id && r.Name == "Manager"
                        select 1).Any()
                select u;
        // =========================================================================

        // 2) Search
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var kw = q.Search.Trim().ToLower();
            users = users.Where(u =>
                (u.Email ?? "").ToLower().Contains(kw) ||
                (u.FullName ?? "").ToLower().Contains(kw));
        }

        // 3) IsActive
        if (q.IsActive.HasValue)
            users = users.Where(u => u.IsActive == q.IsActive.Value);

        // 4) Role filter (lọc TRƯỚC khi Count/Skip/Take)
        if (!string.IsNullOrWhiteSpace(q.Role))
        {
            var roleName = q.Role.Trim();
            users =
                from u in users
                where
                    (from ur in _db.UserRoles
                     join r in _db.Roles on ur.RoleId equals r.Id
                     where ur.UserId == u.Id && r.Name == roleName
                     select 1).Any()
                select u;
        }

        // 5) Sort
        users = (q.SortBy?.ToLower(), q.SortDir?.ToLower()) switch
        {
            ("email", "asc") => users.OrderBy(u => u.Email),
            ("email", _) => users.OrderByDescending(u => u.Email),
            ("fullname", "asc") => users.OrderBy(u => u.FullName),
            ("fullname", _) => users.OrderByDescending(u => u.FullName),
            // AppUser không có CreatedAt -> tạm theo Id
            ("createdat", "asc") => users.OrderBy(u => u.Id),
            _ => users.OrderByDescending(u => u.Id)
        };

        // 6) Count sau khi đã lọc đầy đủ
        var total = await users.CountAsync();

        // 7) Paging
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, 200);
        var pageUsers = await users
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        // 8) Lấy roles cho danh sách user trên trang bằng 1 query
        var userIds = pageUsers.Select(u => u.Id).ToList();
        var rolePairs = await (
            from ur in _db.UserRoles
            join r in _db.Roles on ur.RoleId equals r.Id
            where userIds.Contains(ur.UserId)
            select new { ur.UserId, r.Name }
        ).ToListAsync();

        var rolesMap = rolePairs
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name!).ToList() as IEnumerable<string>);

        // 9) Map DTO
        var items = pageUsers.Select(u => new UserSummaryDto
        {
            Id = u.Id,
            Email = u.Email,
            FullName = u.FullName,
            EmailConfirmed = u.EmailConfirmed,
            IsActive = u.IsActive,
            Roles = rolesMap.TryGetValue(u.Id, out var rs) ? rs : Enumerable.Empty<string>()
        }).ToList();

        return new PagedResult<UserSummaryDto>
        {
            Items = items,
            Page = page,
            PageSize = size,
            TotalItems = total
        };
    }

    public async Task<UserDetailDto?> GetByIdAsync(string userId)
    {
        var u = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (u == null) return null;

        var roles = await _userManager.GetRolesAsync(u);
        return new UserDetailDto
        {
            Id = u.Id,
            Email = u.Email,
            FullName = u.FullName,
            EmailConfirmed = u.EmailConfirmed,
            IsActive = u.IsActive,
            LockoutEnd = u.LockoutEnd,
            CreatedAt = default, // AppUser hiện không có CreatedAt
            Roles = roles
        };
    }

    public async Task<ServiceResult<UserDetailDto>> CreateAsync(CreateUserDto dto)
    {
        var pwd = string.IsNullOrWhiteSpace(dto.Password)
            ? GenerateRandomPassword(10)
            : dto.Password!;

        // ⚙️ Kiểm tra nếu user đã tồn tại → update lại thông tin + roles
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            // ✅ Cập nhật các trường thông tin
            existingUser.FullName = dto.FullName ?? existingUser.FullName;
            existingUser.IsActive = dto.IsActive;

            await _userManager.UpdateAsync(existingUser);

            // ✅ Cập nhật lại roles (xóa role cũ → thêm role mới)
            if (dto.Roles != null && dto.Roles.Any())
            {
                var currentRoles = await _userManager.GetRolesAsync(existingUser);
                if (currentRoles.Any())
                    await _userManager.RemoveFromRolesAsync(existingUser, currentRoles);

                foreach (var role in dto.Roles.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (!await _roleManager.RoleExistsAsync(role))
                        await _roleManager.CreateAsync(new IdentityRole(role));
                }

                await _userManager.AddToRolesAsync(existingUser, dto.Roles);
            }

            var updatedRoles = await _userManager.GetRolesAsync(existingUser);

            return new ServiceResult<UserDetailDto>
            {
                Success = true,
                Message = "Existing user updated successfully.",
                Data = new UserDetailDto
                {
                    Id = existingUser.Id,
                    Email = existingUser.Email,
                    FullName = existingUser.FullName,
                    EmailConfirmed = existingUser.EmailConfirmed,
                    IsActive = existingUser.IsActive,
                    LockoutEnd = existingUser.LockoutEnd,
                    CreatedAt = default, // nếu chưa có trường này thì để nguyên
                    Roles = updatedRoles
                }
            };
        }

        // ⚙️ Nếu chưa tồn tại → tạo mới user
        var user = new AppUser
        {
            Email = dto.Email,
            UserName = dto.Email,
            FullName = dto.FullName,
            IsActive = dto.IsActive,
            EmailConfirmed = true
        };

        var create = await _userManager.CreateAsync(user, pwd);
        if (!create.Succeeded)
        {
            return new ServiceResult<UserDetailDto>
            {
                Success = false,
                Message = string.Join("; ", create.Errors.Select(e => e.Description))
            };
        }

        // ✅ Tạo role nếu chưa có
        foreach (var role in dto.Roles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));
        }

        // ✅ Gán role cho user
        if (dto.Roles.Any())
            await _userManager.AddToRolesAsync(user, dto.Roles);

        var roles = await _userManager.GetRolesAsync(user);

        return new ServiceResult<UserDetailDto>
        {
            Success = true,
            Message = "User created.",
            Data = new UserDetailDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                EmailConfirmed = user.EmailConfirmed,
                IsActive = user.IsActive,
                LockoutEnd = user.LockoutEnd,
                CreatedAt = default, // nếu DB có CreatedAt thì bạn set ở đây
                Roles = roles
            }
        };
    }


    public async Task<ServiceResult<bool>> UpdateAsync(UpdateUserDto dto)
    {
        var u = await _userManager.FindByIdAsync(dto.Id);
        if (u == null) return ServiceResult<bool>.Fail("User not found.");

        if (!string.IsNullOrWhiteSpace(dto.FullName))
            u.FullName = dto.FullName!.Trim();

        if (!string.IsNullOrWhiteSpace(dto.Email) &&
            !string.Equals(u.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
        {
            u.Email = dto.Email!.Trim();
            u.UserName = u.Email;
            u.NormalizedEmail = _userManager.NormalizeEmail(u.Email);
            u.NormalizedUserName = _userManager.NormalizeName(u.UserName);
        }

        if (dto.IsActive.HasValue)
            u.IsActive = dto.IsActive.Value;

        var res = await _userManager.UpdateAsync(u);
        return res.Succeeded
            ? ServiceResult<bool>.Ok(true, "Updated.")
            : ServiceResult<bool>.Fail(string.Join("; ", res.Errors.Select(e => e.Description)));
    }

    public async Task<ServiceResult<bool>> SetRolesAsync(SetRolesDto dto)
    {
        var u = await _userManager.FindByIdAsync(dto.UserId);
        if (u == null) return ServiceResult<bool>.Fail("User not found.");

        foreach (var role in dto.Roles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));
        }

        var current = await _userManager.GetRolesAsync(u);
        var toRemove = current.Where(r => !dto.Roles.Contains(r, StringComparer.OrdinalIgnoreCase)).ToList();
        var toAdd = dto.Roles.Where(r => !current.Contains(r, StringComparer.OrdinalIgnoreCase)).ToList();

        if (toRemove.Any())
        {
            var rmRes = await _userManager.RemoveFromRolesAsync(u, toRemove);
            if (!rmRes.Succeeded)
                return ServiceResult<bool>.Fail(string.Join("; ", rmRes.Errors.Select(e => e.Description)));
        }

        if (toAdd.Any())
        {
            var addRes = await _userManager.AddToRolesAsync(u, toAdd);
            if (!addRes.Succeeded)
                return ServiceResult<bool>.Fail(string.Join("; ", addRes.Errors.Select(e => e.Description)));
        }

        return ServiceResult<bool>.Ok(true, "Roles updated.");
    }

    public async Task<ServiceResult<bool>> AdminResetPasswordAsync(AdminResetPasswordDto dto)
    {
        var u = await _userManager.FindByIdAsync(dto.UserId);
        if (u == null) return ServiceResult<bool>.Fail("User not found.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(u);
        var res = await _userManager.ResetPasswordAsync(u, token, dto.NewPassword);

        return res.Succeeded
            ? ServiceResult<bool>.Ok(true, "Password reset.")
            : ServiceResult<bool>.Fail(string.Join("; ", res.Errors.Select(e => e.Description)));
    }

    public async Task<ServiceResult<bool>> ToggleLockAsync(ToggleLockDto dto)
    {
        var u = await _userManager.FindByIdAsync(dto.UserId);
        if (u == null) return ServiceResult<bool>.Fail("User not found.");

        if (dto.Lock)
        {
            var minutes = dto.Minutes.GetValueOrDefault(60);
            u.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(minutes);
        }
        else
        {
            u.LockoutEnd = null;
            await _userManager.ResetAccessFailedCountAsync(u);
        }

        var res = await _userManager.UpdateAsync(u);
        return res.Succeeded
            ? ServiceResult<bool>.Ok(true, dto.Lock ? "User locked." : "User unlocked.")
            : ServiceResult<bool>.Fail(string.Join("; ", res.Errors.Select(e => e.Description)));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return new ServiceResult<bool> { Success = false, Message = "User not found." };


        if (!user.IsActive)
            return new ServiceResult<bool> { Success = true, Message = "User already inactive.", Data = true };

        user.IsActive = false;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return new ServiceResult<bool>
            {
                Success = false,
                Message = string.Join("; ", result.Errors.Select(e => e.Description))
            };
        }

        return new ServiceResult<bool>
        {
            Success = true,
            Message = "User has been deactivated (IsActive = false).",
            Data = true
        };
    }


    private static string GenerateRandomPassword(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@$?";
        var rnd = new Random();
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[rnd.Next(chars.Length)]).ToArray());
    }
}
