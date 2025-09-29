using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Services;
using CB_Gift.Services.IService;
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

    public AuthController(SignInManager<AppUser> signIn, UserManager<AppUser> users, ITokenService tokens, IConfiguration config, IAccountService accountService)
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

        // Set JWT vào HttpOnly cookie
        Response.Cookies.Append(AccessTokenCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,                     // true trong production (HTTPS)
            SameSite = SameSiteMode.Strict,    // nếu FE khác domain, đổi sang None (+ HTTPS)
            Expires = expires,
            IsEssential = true                 // tránh bị chặn bởi cookie consent
        });

        return Ok(new AuthResponse(token, user.UserName!, user.Email));
    }

    // POST: /api/auth/logout  -> xoá cookie
    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // Xoá cookie access token
        Response.Cookies.Delete(AccessTokenCookieName, new CookieOptions
        {
            Secure = true,
            SameSite = SameSiteMode.Strict
        });

        return Ok(new { message = "Logged out" });
    }

    // POST: /api/auth/change-password
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
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

    // POST: /api/Auth/Register
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
}
