using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Models;
using CB_Gift.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CB_Gift.Services
{
    public class JwtTokenService : ITokenService
    {
        private readonly IConfiguration _config;
        private readonly UserManager<AppUser> _userManager;
        private readonly CBGiftDbContext _context;

        public JwtTokenService(IConfiguration config, UserManager<AppUser> userManager, CBGiftDbContext context)
        {
            _config = config;
            _userManager = userManager;
            _context = context;
        }

        public async Task<string> CreateTokenAsync(AppUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            Console.WriteLine("Roles:"+roles.ToString());
            var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.Name, user.UserName ?? ""),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new("fullName", user.FullName ?? "")
        };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiresMinutes"] ?? "60"));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // 1. Tạo Refresh Token ngẫu nhiên
        public async Task<RefreshToken> GenerateRefreshTokenAsync(string userId)
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);

            var refreshToken = new RefreshToken
            {
                Token = Convert.ToBase64String(randomNumber),
                UserId = userId,
                Expires = DateTime.UtcNow.AddDays(7), // Token sống 7 ngày
                Created = DateTime.UtcNow
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return refreshToken;
        }

        // 2. Validate Token (Có xoay vòng token - Rotation)
        public async Task<ServiceResult<AppUser>> ValidateRefreshTokenAsync(string token)
        {
            var storedToken = await _context.RefreshTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Token == token);

            if (storedToken == null)
                return new ServiceResult<AppUser> { Success = false, Message = "Token not found" };

            if (!storedToken.IsActive)
                return new ServiceResult<AppUser> { Success = false, Message = "Token expired or revoked" };

            // Quan trọng: Khi token được dùng để refresh, ta hủy nó ngay (Rotation)
            // Để lần sau phải dùng token mới -> Nếu hacker trộm token cũ cũng vô dụng.
            storedToken.Revoked = DateTime.UtcNow;
            _context.RefreshTokens.Update(storedToken);
            await _context.SaveChangesAsync();

            return new ServiceResult<AppUser> { Success = true, Data = storedToken.User };
        }

        // 3. Hủy Token (Logout)
        public async Task RevokeRefreshTokenAsync(string token, string ipAddress = null)
        {
            var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == token);
            if (storedToken != null)
            {
                storedToken.Revoked = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        public async Task RevokeAllRefreshTokensAsync(string userId)
        {
            // Tìm tất cả token của user này mà chưa bị hủy và chưa hết hạn
            var activeTokens = await _context.RefreshTokens
                .Where(x => x.UserId == userId && x.Revoked == null && x.Expires > DateTime.UtcNow)
                .ToListAsync();

            if (activeTokens.Any())
            {
                var now = DateTime.UtcNow;
                foreach (var token in activeTokens)
                {
                    token.Revoked = now; // Đánh dấu đã hủy
                }

                // Update một lần
                _context.RefreshTokens.UpdateRange(activeTokens);
                await _context.SaveChangesAsync();
            }
        }
    }
}
