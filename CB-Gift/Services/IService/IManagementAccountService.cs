
using CB_Gift.DTOs;

namespace CB_Gift.Services.IService;

public interface IManagementAccountService
{
    Task<PagedResult<UserSummaryDto>> GetUsersAsync(UserQuery query);
    Task<UserDetailDto?> GetByIdAsync(string userId);
    Task<ServiceResult<UserDetailDto>> CreateAsync(CreateUserDto dto);
    Task<ServiceResult<bool>> UpdateAsync(UpdateUserDto dto);
    Task<ServiceResult<bool>> SetRolesAsync(SetRolesDto dto);
    Task<ServiceResult<bool>> AdminResetPasswordAsync(AdminResetPasswordDto dto);
    Task<ServiceResult<bool>> ToggleLockAsync(ToggleLockDto dto);
    Task<ServiceResult<bool>> DeleteAsync(string userId);
}
