// File: CB_Gift/Controllers/ManagementAccountsController.cs
using CB_Gift.DTOs;
using CB_Gift.Services.Email;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CB_Gift.Controllers;

[ApiController]
[Route("api/management/accounts")]
[Authorize(Roles = "Manager")]
public class ManagementAccountsController : ControllerBase
{
    private readonly IManagementAccountService _svc;
    private readonly ITokenService _tokens;


    public ManagementAccountsController(IManagementAccountService svc, ITokenService tokens)
    {
        _svc = svc;
        _tokens = tokens;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<UserSummaryDto>>> Get([FromQuery] UserQuery q)
        => Ok(await _svc.GetUsersAsync(q));

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDetailDto>> GetById(string id)
    {
        var res = await _svc.GetByIdAsync(id);
        return res is null ? NotFound() : Ok(res);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        var res = await _svc.CreateAsync(dto);
        return !res.Success ? BadRequest(res) : Ok(res);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateUserDto dto)
    {
        var res = await _svc.UpdateAsync(dto);
        if (!res.Success)
        {
            BadRequest(res);
        }
        await _tokens.RevokeAllRefreshTokensAsync(dto.Id);
        return Ok(res);
    }

    [HttpPut("roles")]
    public async Task<IActionResult> SetRoles([FromBody] SetRolesDto dto)
    {
        var res = await _svc.SetRolesAsync(dto);
        if (!res.Success)
        {
            BadRequest(res);
        }
        await _tokens.RevokeAllRefreshTokensAsync(dto.UserId);
        return Ok(res);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> AdminResetPassword([FromBody] AdminResetPasswordDto dto)
    {
        var res = await _svc.AdminResetPasswordAsync(dto);
        return !res.Success ? BadRequest(res) : Ok(res);
    }

    [HttpPost("lock")]
    public async Task<IActionResult> ToggleLock([FromBody] ToggleLockDto dto)
    {
        var res = await _svc.ToggleLockAsync(dto);
        return !res.Success ? BadRequest(res) : Ok(res);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var res = await _svc.DeleteAsync(id);
        if (!res.Success)
        {
            BadRequest(res);
        }
        await _tokens.RevokeAllRefreshTokensAsync(id);
        return Ok(res);
    }
}
