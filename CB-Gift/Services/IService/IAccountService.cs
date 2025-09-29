using Microsoft.AspNetCore.Identity.Data;
using CB_Gift.DTOs;

namespace CB_Gift.Services.IService
{
    public interface IAccountService
    {
        Task<ServiceResult<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request);
        Task SendResetPasswordEmailAsync(string email, string resetLink);
    }
}
