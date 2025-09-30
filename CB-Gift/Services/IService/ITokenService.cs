using CB_Gift.Data;

namespace CB_Gift.Services.Email
{
    public interface ITokenService
    {
        Task<string> CreateTokenAsync(AppUser user);
    }
}
