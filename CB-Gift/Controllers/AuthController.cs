﻿using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Services.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CB_Gift.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const string AccessTokenCookieName = "access_token";

    private readonly SignInManager<AppUser> _signIn;
    private readonly UserManager<AppUser> _users;
    private readonly ITokenService _tokens;
    private readonly IAccountService _accountService;
    private readonly IConfiguration _config;

    public AuthController(
        SignInManager<AppUser> signIn,
        UserManager<AppUser> users,
        ITokenService tokens,
        IConfiguration config,
        IAccountService accountService)
    {
        _signIn = signIn;
        _users = users;
        _tokens = tokens;
        _config = config;
        _accountService = accountService;
    }

    // POST: /api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var user = await _users.FindByNameAsync(dto.UserNameOrEmail)
                   ?? await _users.FindByEmailAsync(dto.UserNameOrEmail);

        if (user is null || !user.IsActive)
            return Unauthorized(new { message = "Invalid credentials." });

        var ok = await _signIn.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: false);
        if (!ok.Succeeded)
            return Unauthorized(new { message = "Invalid credentials." });

        var token = await _tokens.CreateTokenAsync(user);

        var minutes = int.Parse(_config["Jwt:ExpiresMinutes"] ?? "60");
        var expires = DateTimeOffset.UtcNow.AddMinutes(minutes);

        Response.Cookies.Append(AccessTokenCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None, // Be và Fe khác domain thì None sẽ gửi được cookie từ Be qua Fe.
            Expires = expires,
            IsEssential = true
        });

        return Ok(new AuthResponse(token, user.UserName!, user.Email));
    }

    // POST: /api/auth/logout
    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AccessTokenCookieName, new CookieOptions
        {
            Secure = true,
            SameSite = SameSiteMode.None
        });

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

        return Ok(new { message = "Password changed." });
    }
    
    // POST: /api/Auth/register
    //[Authorize(Roles = "Manager")]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {

        var result = await _accountService.RegisterAsync(request);
        if (!result.Success)
        {
            return BadRequest(result.Message);
        }

        return Ok(result.Data);
    }

    // POST: /api/auth/forgot-password
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        // Tất cả logic đã được chuyển vào service
        var result = await _accountService.SendPasswordResetOtpAsync(dto);

        if (!result.Success)
        {
            // Trả về BadRequest nếu có lỗi cụ thể (ví dụ: user bị khóa)
            return BadRequest(new { message = result.Message });
        }

        // Luôn trả về OK để bảo mật, ngay cả khi email không tồn tại
        return Ok(new { message = "If your email is registered, you will receive an OTP." });
    }

    // POST: /api/auth/reset-password
    [HttpPost("reset-password")]
    [AllowAnonymous]
    // Sử dụng DTO mới
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordWithOtpDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Tất cả logic đã được chuyển vào service
        var result = await _accountService.ResetPasswordWithOtpAsync(dto);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.Message,
            });
        }

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
    /// <summary>
    /// Lấy danh sách tất cả các Seller trong hệ thống.
    /// </summary> api/auth/all-sellers
    [Authorize(Roles = "Staff,Manager")]
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
            return StatusCode(500, new { message = "Đã xảy ra lỗi không mong muốn khi lấy danh sách seller.", error = ex.Message });
        }
    }


}
