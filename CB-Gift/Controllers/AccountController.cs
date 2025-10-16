
using CB_Gift.DTOs;
using CB_Gift.Services.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CB_Gift.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize] 
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _svc;
        public AccountController(IAccountService svc) => _svc = svc;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? q) =>
            Ok(await _svc.GetAllAsync(q));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var data = await _svc.GetByIdAsync(id);
            return data == null ? NotFound() : Ok(data);
        }

        [HttpPost]
        [AllowAnonymous] 
        public async Task<IActionResult> Create([FromBody] AccountCreateDto dto)
        {
            var (ok, error, data) = await _svc.CreateAsync(dto);
            if (!ok) return BadRequest(new { error });
            return CreatedAtAction(nameof(GetById), new { id = data!.Id }, data);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] AccountUpdateDto dto)
        {
            var (ok, error) = await _svc.UpdateAsync(id, dto);
            if (!ok) return BadRequest(new { error });
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var (ok, error) = await _svc.DeleteAsync(id);
            if (!ok) return BadRequest(new { error });
            return NoContent();
        }
    }
}
