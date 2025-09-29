using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Identity;
using System.Net;
using System.Net.Mail;

namespace CB_Gift.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _config;

        public AccountService(UserManager<AppUser> userManager, IConfiguration config)
        {
            _userManager = userManager;
            _config = config;
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
            var subject = "Welcome to CB-Gift";
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

            await SendEmailAsync(email, subject, body);
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

            await SendEmailAsync(email, subject, body);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpServer = _config["Email:SmtpServer"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var senderEmail = _config["Email:Sender"];
            var senderPassword = _config["Email:Password"];

            using var client = new SmtpClient(smtpServer, smtpPort)
            {
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, "CB-Gift Support"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
        }


    }
}
