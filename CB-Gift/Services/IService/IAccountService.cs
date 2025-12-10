using Microsoft.AspNetCore.Identity.Data;
using CB_Gift.DTOs;

namespace CB_Gift.Services.Email
{
    public interface IAccountService
    {
        Task<ServiceResult<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request);
        Task<ServiceResult<ForgotPasswordDto>> SendPasswordResetOtpAsync(ForgotPasswordDto dto);
        Task<ServiceResult<ResetPasswordWithOtpDto>> ResetPasswordWithOtpAsync(ResetPasswordWithOtpDto dto);
        Task<IEnumerable<SellerDto>> GetAllSellersAsync();
        Task<ServiceResult<bool>> VerifyOtpAsync(string email, string otp);

        Task<IEnumerable<DesignerDto>> GetAllDesignersAsync();

        Task<ServiceResult<bool>> UpdateProfileAsync(string userId, UpdateProfileDto dto);
    }
}
