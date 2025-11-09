using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Services.Email;
using Microsoft.AspNetCore.Identity;
using System.Net;
using System.Net.Mail;

namespace CB_Gift.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailSender _emailSender;

        public AccountService(UserManager<AppUser> userManager, IEmailSender emailSender)
        {
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

        private string GenerateRandomPassword(int length = 8)
        {
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*";
            var allChars = upper + lower + digits + special;

            var random = new Random();
            var passwordChars = new char[length];

            // Bắt buộc có ít nhất 1 ký tự mỗi loại
            passwordChars[0] = upper[random.Next(upper.Length)];
            passwordChars[1] = lower[random.Next(lower.Length)];
            passwordChars[2] = digits[random.Next(digits.Length)];
            passwordChars[3] = special[random.Next(special.Length)];

            // Các ký tự còn lại ngẫu nhiên
            for (int i = 4; i < length; i++)
                passwordChars[i] = allChars[random.Next(allChars.Length)];

            // Trộn để không cố định vị trí
            return new string(passwordChars.OrderBy(_ => random.Next()).ToArray());
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

        private string GenerateRandomOtp(int length = 6)
        {
            const string chars = "0123456789";
            var random = new Random();
            var otp = new char[length];
            for (int i = 0; i < length; i++)
            {
                otp[i] = chars[random.Next(chars.Length)];
            }
            return new string(otp);
        }

        public async Task<ServiceResult<ForgotPasswordDto>> SendPasswordResetOtpAsync(ForgotPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                // Vẫn trả về thông báo thành công chung để tránh lộ thông tin email nào đã đăng ký
                return new ServiceResult<ForgotPasswordDto> { Success = true, Message = "If your email is registered, you will receive an OTP." };
            }

            if (!user.IsActive)
            {
                return new ServiceResult<ForgotPasswordDto> { Success = false, Message = "User is inactive." };
            }

            // Tạo OTP và set thời gian hết hạn (2 phút)
            var otp = GenerateRandomOtp(6);
            user.PasswordResetOtp = otp;
            user.PasswordResetOtpExpiry = DateTime.UtcNow.AddMinutes(2); // Yêu cầu 2 phút

            // Lưu OTP vào database
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return new ServiceResult<ForgotPasswordDto> { Success = false, Message = "Failed to update OTP." };
            }

            // Gửi email chứa OTP
            await SendResetPasswordEmailAsync(user.Email, otp);

            return new ServiceResult<ForgotPasswordDto> { Success = true, Message = "OTP has been sent to your email." };
        }

        public async Task SendResetPasswordEmailAsync(string email, string otp)
        {
            var subject = "Reset your CB-Gift password (OTP)";
            var body = $@"
                <h2>Password Reset Request</h2>
                <p>Dear {email},</p>
                <p>We received a request to reset your password. Here is your One-Time Password (OTP):</p>
                <h3 style='font-size: 24px; letter-spacing: 3px; margin: 10px 0;'>{otp}</h3>
                <p>This OTP is valid for <strong>2 minutes</strong>.</p>
                <p>If you did not request this, please ignore this email.</p>
                <br/>
                <p>Best regards,</p>
                <p>The CB-Gift Team</p>";

            await _emailSender.SendAsync(email, subject, body);
        }

        // 1️⃣ Xác thực OTP
        public async Task<ServiceResult<bool>> VerifyOtpAsync(string email, string otp)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return new ServiceResult<bool> { Success = false, Message = "User not found." };

            // Kiểm tra OTP
            if (user.PasswordResetOtp != otp)
                return new ServiceResult<bool> { Success = false, Message = "Invalid OTP." };

            // Kiểm tra hạn OTP
            if (user.PasswordResetOtpExpiry == null || DateTime.UtcNow > user.PasswordResetOtpExpiry)
                return new ServiceResult<bool> { Success = false, Message = "OTP has expired." };

            // ✅ OTP hợp lệ, không reset password ở đây
            return new ServiceResult<bool> { Success = true, Message = "OTP verified successfully.", Data = true };
        }


        // 2️⃣ Đặt lại mật khẩu sau khi OTP đã xác thực
        public async Task<ServiceResult<ResetPasswordWithOtpDto>> ResetPasswordWithOtpAsync(ResetPasswordWithOtpDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return new ServiceResult<ResetPasswordWithOtpDto> { Success = false, Message = "User not found." };

            // Kiểm tra lại một lần nữa cho an toàn
            if (user.PasswordResetOtp != dto.Otp)
                return new ServiceResult<ResetPasswordWithOtpDto> { Success = false, Message = "Invalid OTP." };

            if (user.PasswordResetOtpExpiry == null || DateTime.UtcNow > user.PasswordResetOtpExpiry)
                return new ServiceResult<ResetPasswordWithOtpDto> { Success = false, Message = "OTP has expired." };

            // Tạo token của Identity
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"❌ Password reset error: {error.Code} - {error.Description}");
                }

                var errorString = string.Join("; ", result.Errors.Select(e => e.Description));
                return new ServiceResult<ResetPasswordWithOtpDto>
                {
                    Success = false,
                    Message = $"Reset password failed: {errorString}"
                };
            }

            // Xóa OTP sau khi reset thành công
            user.PasswordResetOtp = null;
            user.PasswordResetOtpExpiry = null;
            await _userManager.UpdateAsync(user);

            return new ServiceResult<ResetPasswordWithOtpDto>
            {
                Success = true,
                Message = "Password has been reset successfully."
            };
        }


        /// Lấy danh sách tất cả người dùng có vai trò là "Seller".
        public async Task<IEnumerable<SellerDto>> GetAllSellersAsync()
        {
            // Lấy danh sách tất cả người dùng thuộc vai trò "Seller"
            var sellers = await _userManager.GetUsersInRoleAsync("Seller");

            // Sử dụng LINQ để biến đổi danh sách AppUser sang danh sách SellerDto
            return sellers.Select(user => new SellerDto
            {
                SellerId = user.Id,
                SellerName = user.FullName
            }).ToList();
        }

        /// <summary>
        /// Lấy danh sách tất cả người dùng có vai trò là "Designer".
        /// </summary>
        public async Task<IEnumerable<DesignerDto>> GetAllDesignersAsync()
        {
            // Lấy danh sách tất cả người dùng thuộc vai trò "Designer"
            var designers = await _userManager.GetUsersInRoleAsync("Designer");

            // Biến đổi danh sách AppUser sang danh sách DesignerDto
            return designers.Select(user => new DesignerDto
            {
                DesignerId = user.Id,
                DesignerName = user.FullName
            }).ToList();
        }




    }
}
