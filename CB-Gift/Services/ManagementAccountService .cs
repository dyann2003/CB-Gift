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
            // AppUser không có CreatedAt -> tạm theo Id (chuỗi tăng không đảm bảo thời gian, nhưng ổn tạm)
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

        foreach (var role in dto.Roles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));
        }
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
                CreatedAt = default, // chưa có trường này
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
        var u = await _userManager.FindByIdAsync(userId);
        if (u == null) return ServiceResult<bool>.Fail("User not found.");

        // Hard delete (AppUser hiện không có IsDeleted)
        var res = await _userManager.DeleteAsync(u);
        return res.Succeeded
            ? ServiceResult<bool>.Ok(true, "User deleted.")
            : ServiceResult<bool>.Fail(string.Join("; ", res.Errors.Select(e => e.Description)));
    }

    private static string GenerateRandomPassword(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@$?";
        var rnd = new Random();
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[rnd.Next(chars.Length)]).ToArray());
    }
}
