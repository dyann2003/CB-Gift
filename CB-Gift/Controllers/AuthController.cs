using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CB_Gift.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<AppUser> _signIn;
    private readonly UserManager<AppUser> _users;
    private readonly ITokenService _tokens;

    public AuthController(SignInManager<AppUser> signIn, UserManager<AppUser> users, ITokenService tokens)
    {
        _signIn = signIn;
        _users = users;
        _tokens = tokens;
    }

    // POST: /api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _users.FindByNameAsync(dto.UserNameOrEmail)
                   ?? await _users.FindByEmailAsync(dto.UserNameOrEmail);

        if (user is null || !user.IsActive) return Unauthorized();

        var ok = await _signIn.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: false);
        if (!ok.Succeeded) return Unauthorized();

        var token = await _tokens.CreateTokenAsync(user);
        return Ok(new AuthResponse(token, user.UserName!, user.Email));
    }

    // POST: /api/auth/logout (JWT: client xoá token là đủ)
    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
        => Ok(new { message = "Logout OK. Hãy xoá token ở client." });

    // POST: /api/auth/change-password
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var rs = await _users.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!rs.Succeeded) return BadRequest(rs.Errors);

        return Ok(new { message = "Password changed." });
    }
}
