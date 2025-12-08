using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Models;

namespace CB_Gift.Services.Email
{
    public interface ITokenService
    {
        Task<string> CreateTokenAsync(AppUser user);
        Task<RefreshToken> GenerateRefreshTokenAsync(string userId);
        Task<ServiceResult<AppUser>> ValidateRefreshTokenAsync(string token);
        Task RevokeRefreshTokenAsync(string token, string ipAddress = null);
    }
}
