using CB_Gift.DTOs;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CB_Gift.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReprintController : ControllerBase
    {
        private readonly IReprintService _reprintService;

        public ReprintController(IReprintService reprintService)
        {
            _reprintService = reprintService;
        }

        // User submit reprint request
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitReprint([FromBody] ReprintSubmitDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _reprintService.SubmitReprintAsync(dto);
            return Ok(new { message = "Reprint request submitted successfully." });
        }

        // Manager approve
        [HttpPost("approve")]
        public async Task<IActionResult> ApproveReprint([FromBody] ReprintManagerDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _reprintService.ApproveReprintAsync(dto);
            return Ok(new { message = "Reprint request approved and new order created." });
        }

        // Manager reject
        [HttpPost("reject")]
        public async Task<IActionResult> RejectReprint([FromBody] ReprintManagerDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _reprintService.RejectReprintAsync(dto);
            return Ok(new { message = "Reprint request rejected and order restored to previous status." });
        }
    }
}
