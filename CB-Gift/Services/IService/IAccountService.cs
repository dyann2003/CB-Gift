using CB_Gift.Data;
using CB_Gift.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;

namespace CB_Gift.Services.Email
{
    public interface IAccountService
    {
        Task<ServiceResult<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request);
        Task SendResetPasswordEmailAsync(string email, string resetLink);
        Task<AccountViewDto?> GetByIdAsync(string id);
        Task<IEnumerable<AccountViewDto>> GetAllAsync(string? keyword = null);
        Task<(bool ok, string? error, AccountViewDto? data)> CreateAsync(AccountCreateDto dto);
        Task<(bool ok, string? error)> UpdateAsync(string id, AccountUpdateDto dto);
        Task<(bool ok, string? error)> DeleteAsync(string id);
    }
}
