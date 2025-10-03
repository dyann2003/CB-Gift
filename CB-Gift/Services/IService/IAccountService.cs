using Microsoft.AspNetCore.Identity.Data;
using CB_Gift.DTOs;

namespace CB_Gift.Services.Email
{
    public interface IAccountService
    {
        Task<ServiceResult<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request);
        Task SendResetPasswordEmailAsync(string email, string resetLink);
    }
}
