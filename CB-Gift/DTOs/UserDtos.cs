// File: CB_Gift/DTOs/Accounts/UserDtos.cs
using System.ComponentModel.DataAnnotations;

namespace CB_Gift.DTOs;

public class UserSummaryDto
{
    public string Id { get; set; } = default!;
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool IsActive { get; set; }
    public IEnumerable<string> Roles { get; set; } = Enumerable.Empty<string>();
}

public class UserDetailDto : UserSummaryDto
{
    public DateTimeOffset? LockoutEnd { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUserDto
{
    [Required, EmailAddress] public string Email { get; set; } = default!;
    public string? FullName { get; set; }
    /// <summary>Để trống = hệ thống tạo mật khẩu ngẫu nhiên.</summary>
    public string? Password { get; set; }
    public List<string> Roles { get; set; } = new();
    public bool IsActive { get; set; } = true;
}

public class UpdateUserDto
{
    [Required] public string Id { get; set; } = default!;
    public string? FullName { get; set; }
    [EmailAddress] public string? Email { get; set; }
    public bool? IsActive { get; set; }
}

public class SetRolesDto
{
    [Required] public string UserId { get; set; } = default!;
    [Required] public List<string> Roles { get; set; } = new();
}

public class AdminResetPasswordDto
{
    [Required] public string UserId { get; set; } = default!;
    [Required, StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; } = default!;
}

public class ToggleLockDto
{
    [Required] public string UserId { get; set; } = default!;
    public bool Lock { get; set; }
    public int? Minutes { get; set; } // mặc định 60 nếu null
}

public class UserQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }    // email/fullname
    public string? Role { get; set; }      // lọc theo role
    public bool? IsActive { get; set; }    // lọc theo trạng thái
    public string? SortBy { get; set; } = "createdAt"; // createdAt|email|fullName
    public string? SortDir { get; set; } = "desc";      // asc|desc
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}
