using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Services.Email;
using CB_Gift.Validators;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;

namespace CB_Gift.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailSender _emailSender;
     
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountService(UserManager<AppUser> userManager, IEmailSender emailSender, RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        public async Task<ServiceResult<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request)
        {
            var userExists = await _userManager.FindByEmailAsync(request.Email);
            if (userExists != null)
            {
                return new ServiceResult<RegisterResponseDto>
                {
                    Success = false,
                    Message = "Email already registered."
                };
            }

            // Generate random password
            var password = GenerateRandomPassword(6);

            var user = new AppUser
            {
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return new ServiceResult<RegisterResponseDto>
                {
                    Success = false,
                    Message = $"User creation failed: {errors}"
                };
            }

            // Send welcome email
            await SendWelcomeEmailAsync(request.Email, password);

            var responseDto = new RegisterResponseDto
            {
                Email = request.Email,
                TemporaryPassword = password,
                Message = "Account created successfully. Please check your email."
            };

            return new ServiceResult<RegisterResponseDto>
            {
                Success = true,
                Message = "Account created successfully.",
                Data = responseDto
            };
        }

        private string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var password = new char[length];
            for (int i = 0; i < length; i++)
            {
                password[i] = chars[random.Next(chars.Length)];
            }
            return new string(password);
        }

        private async Task SendWelcomeEmailAsync(string email, string password)
        {
            var subject = "Welcome to CB-Gift!";
            var body = $@"
                <h2>Welcome to CB-Gift!</h2>
                <p>Dear {email},</p>
                <p>Your account has been successfully created.</p>
                <p><strong>Email:</strong> {email}</p>
                <p><strong>Password:</strong> {password}</p>
                <p>Please change your password after logging in for the first time.</p>
                <br/>
                <p>Best regards,</p>
                <p>The CB-Gift Team</p>";

            await _emailSender.SendAsync(email, subject, body);
        }

        public async Task SendResetPasswordEmailAsync(string email, string resetLink)
        {
            var subject = "Reset your CB-Gift password";
            var body = $@"
                <h2>Password Reset Request</h2>
                <p>Dear {email},</p>
                <p>We received a request to reset your password. Please click the link below:</p>
                <p><a href='{resetLink}'>Reset Password</a></p>
                <p>If you did not request this, please ignore this email.</p>
                <br/>
                <p>Best regards,</p>
                <p>The CB-Gift Team</p>";

            await _emailSender.SendAsync(email, subject, body);
        }
        public async Task<AccountViewDto?> GetByIdAsync(string id)
        {
            var u = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == id);
            if (u == null) return null;
            var roles = await _userManager.GetRolesAsync(u);
            return ToView(u, roles.FirstOrDefault());
        }

        public async Task<IEnumerable<AccountViewDto>> GetAllAsync(string? keyword = null)
        {
            var query = _userManager.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(x => x.FullName!.Contains(keyword) || x.Email!.Contains(keyword));

            var list = await query.ToListAsync();
            var result = new List<AccountViewDto>(list.Count);
            foreach (var u in list)
            {
                var r = (await _userManager.GetRolesAsync(u)).FirstOrDefault();
                result.Add(ToView(u, r));
            }
            return result;
        }

        public async Task<(bool ok, string? error, AccountViewDto? data)> CreateAsync(AccountCreateDto dto)
        {
            var v = AccountValidators.ValidateCreate(dto);
            if (!v.ok) return (false, v.error, null);

            var roleExists = await _roleManager.RoleExistsAsync(dto.Role);
            if (!roleExists) return (false, "Role not found", null);

            var existed = await _userManager.FindByEmailAsync(dto.Email);
            if (existed != null) return (false, "Email already exists", null);

            var user = new AppUser
            {
                Id = Guid.NewGuid().ToString(),      // UUID (string)
                UserName = dto.Email,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                FullName = dto.FullName,
                IsActive = dto.IsActive
            };

            var create = await _userManager.CreateAsync(user, dto.Password);
            if (!create.Succeeded) return (false, string.Join("; ", create.Errors.Select(e => e.Description)), null);

            var addRole = await _userManager.AddToRoleAsync(user, dto.Role);
            if (!addRole.Succeeded) return (false, string.Join("; ", addRole.Errors.Select(e => e.Description)), null);

            return (true, null, ToView(user, dto.Role));
        }

        public async Task<(bool ok, string? error)> UpdateAsync(string id, AccountUpdateDto dto)
        {
            var v = AccountValidators.ValidateUpdate(dto);
            if (!v.ok) return (false, v.error);

            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == id);
            if (user == null) return (false, "User not found");

            user.FullName = dto.FullName;
            user.PhoneNumber = dto.PhoneNumber;
            user.IsActive = dto.IsActive;

            var upd = await _userManager.UpdateAsync(user);
            if (!upd.Succeeded) return (false, string.Join("; ", upd.Errors.Select(e => e.Description)));

            // update role (1 role)
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(dto.Role))
            {
                if (currentRoles.Any())
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                var roleExists = await _roleManager.RoleExistsAsync(dto.Role);
                if (!roleExists) return (false, "Role not found");
                var add = await _userManager.AddToRoleAsync(user, dto.Role);
                if (!add.Succeeded) return (false, string.Join("; ", add.Errors.Select(e => e.Description)));
            }

            return (true, null);
        }

        public async Task<(bool ok, string? error)> DeleteAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return (false, "User not found");

            var del = await _userManager.DeleteAsync(user);
            if (!del.Succeeded) return (false, string.Join("; ", del.Errors.Select(e => e.Description)));

            return (true, null);
        }

        private static AccountViewDto ToView(AppUser u, string? role) =>
            new AccountViewDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email!,
                PhoneNumber = u.PhoneNumber,
                Role = role ?? string.Empty,
                IsActive = u.IsActive
            };
    }
}
