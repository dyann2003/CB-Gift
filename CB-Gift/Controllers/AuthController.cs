using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Services.Email;
using CB_Gift.Services.IService;
using DocumentFormat.OpenXml.Office2016.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CB_Gift.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    // Định nghĩa tên Cookie
    private const string AccessCookie = "access_token";
    private const string RefreshCookie = "refresh_token";

    private readonly SignInManager<AppUser> _signIn;
    private readonly UserManager<AppUser> _users;
    private readonly ITokenService _tokens;
    private readonly IAccountService _accountService;
    private readonly IConfiguration _config;
    private readonly IInvoiceService _invoiceService;

    public AuthController(
        SignInManager<AppUser> signIn,
        UserManager<AppUser> users,
        ITokenService tokens,
        IConfiguration config,
        IAccountService accountService,
        IInvoiceService invoiceService)
    {
        _signIn = signIn;
        _users = users;
        _tokens = tokens;
        _config = config;
        _accountService = accountService;
        _invoiceService = invoiceService;
    }

    // POST: /api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _users.FindByNameAsync(dto.UserNameOrEmail)
                   ?? await _users.FindByEmailAsync(dto.UserNameOrEmail);

        if (user is null) return Unauthorized(new { message = "User not found." });
        if (!user.IsActive) return Unauthorized(new { message = "Account deactivated." });

        var result = await _signIn.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: false);
        if (!result.Succeeded) return Unauthorized(new { message = "Invalid credentials." });

        // 1. Tạo Access Token
        var accessToken = await _tokens.CreateTokenAsync(user);

        // 2. Tạo Refresh Token (Gọi từ TokenService)
        var refreshToken = await _tokens.GenerateRefreshTokenAsync(user.Id);

        // 3. Lưu Cookie
        SetTokenCookies(accessToken, refreshToken.Token);

        return Ok(new
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            UserName = user.UserName,
            Email = user.Email
        });
    }

    // POST: /api/auth/refresh-token
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var refreshToken = request?.RefreshToken ?? Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { message = "No refresh token provided." });

        // 1. Validate Token (Gọi từ TokenService)
        var result = await _tokens.ValidateRefreshTokenAsync(refreshToken);

        if (!result.Success)
        {
            DeleteTokenCookies(); // Token không hợp lệ -> Xóa cookie
            return Unauthorized(new { message = result.Message });
        }

        var user = result.Data;

        // 2. Tạo cặp Token MỚI (Cơ chế Token Rotation)
        var newAccessToken = await _tokens.CreateTokenAsync(user);
        var newRefreshToken = await _tokens.GenerateRefreshTokenAsync(user.Id);

        // 3. Cập nhật Cookie mới
        SetTokenCookies(newAccessToken, newRefreshToken.Token);

        return Ok(new
        {
            accessToken = newAccessToken,
            refreshToken = newRefreshToken.Token
        });
    }

    // POST: /api/auth/logout
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies[RefreshCookie];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            // Thu hồi token trong database
            await _tokens.RevokeRefreshTokenAsync(refreshToken);
        }

        DeleteTokenCookies();
        return Ok(new { message = "Logged out" });
    }

    // POST: /api/auth/change-password
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var user = await _users.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var rs = await _users.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!rs.Succeeded)
            return BadRequest(new
            {
                message = "Change password failed",
                errors = rs.Errors.Select(e => new { e.Code, e.Description })
            });

        var refreshToken = Request.Cookies[RefreshCookie];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _tokens.RevokeRefreshTokenAsync(refreshToken);
        }
        DeleteTokenCookies(); 

        return Ok(new { message = "Password changed. Please login again." });
    }

    // POST: /api/Auth/register
    [Authorize(Roles = "Manager")]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var result = await _accountService.RegisterAsync(request);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { data = result.Data });
    }

    // POST: /api/auth/forgot-password
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest(new { message = "Please enter your email address." });

        var user = await _users.FindByNameAsync(dto.Email)
                   ?? await _users.FindByEmailAsync(dto.Email);

        if (user is null) return Unauthorized(new { message = "Email not found." });
        if (!user.IsActive) return Unauthorized(new { message = "Your account has been deactivated." });

        var result = await _accountService.SendPasswordResetOtpAsync(dto);

        if (!result.Success) return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    // POST: /api/auth/verify-otp
    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _accountService.VerifyOtpAsync(dto.Email, dto.Otp);

        if (!result.Success) return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    // POST: /api/auth/reset-password-with-otp
    [HttpPost("reset-password-with-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPasswordWithOtp([FromBody] ResetPasswordWithOtpDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _accountService.ResetPasswordWithOtpAsync(dto);

        if (!result.Success) return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    // GET: /api/auth/profile
    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> Profile()
    {
        var user = await _users.GetUserAsync(User);
        if (user == null) return Unauthorized();

        return Ok(new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.PhoneNumber,
            user.IsActive,
            user.FullName
        });
    }

    // GET: /api/auth/all-sellers
    [Authorize(Roles = "Staff,Manager,QC")]
    [HttpGet("all-sellers")]
    public async Task<IActionResult> GetAllSellers()
    {
        try
        {
            var sellers = await _accountService.GetAllSellersAsync();
            return Ok(sellers);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi lấy danh sách seller.", error = ex.Message });
        }
    }

    // GET: /api/auth/all-designers
    [Authorize(Roles = "Staff,Manager")]
    [HttpGet("all-designers")]
    public async Task<IActionResult> GetAllDesigners()
    {
        try
        {
            var designers = await _accountService.GetAllDesignersAsync();
            if (designers == null || !designers.Any())
                return NotFound(new { message = "Không tìm thấy designer nào." });

            return Ok(designers);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi lấy danh sách designer.", error = ex.Message });
        }
    }

    // GET: /api/auth/status
    [HttpGet("status")]
    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> GetAccountStatus()
    {
        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sellerId)) return Unauthorized();

        var statusCheck = await _invoiceService.CheckForOverdueInvoiceAsync(sellerId);
        return Ok(statusCheck);
    }

    // --- HELPER METHODS (Private) ---
    private void SetTokenCookies(string accessToken, string refreshToken)
    {
        var accessMinutes = int.Parse(_config["Jwt:ExpiresMinutes"] ?? "60");

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Quan trọng khi chạy HTTPS
            SameSite = SameSiteMode.None,
            IsEssential = true
        };

        // Access Token Cookie (Thời hạn ngắn)
        var accessOpt = cookieOptions;
        accessOpt.Expires = DateTime.UtcNow.AddMinutes(accessMinutes);
        Response.Cookies.Append(AccessCookie, accessToken, accessOpt);

        // Refresh Token Cookie (Thời hạn dài - 7 ngày)
        var refreshOpt = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(7)
        };
        Response.Cookies.Append(RefreshCookie, refreshToken, refreshOpt);
    }

    private void DeleteTokenCookies()
    {
        var options = new CookieOptions { Secure = true, SameSite = SameSiteMode.None };
        Response.Cookies.Delete(AccessCookie, options);
        Response.Cookies.Delete(RefreshCookie, options);
    }
}