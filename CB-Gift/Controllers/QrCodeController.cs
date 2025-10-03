using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CB_Gift.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QrCodeController : ControllerBase
    {
        private readonly IQrCodeService _qrCodeService;

        public QrCodeController(IQrCodeService qrCodeService)
        {
            _qrCodeService = qrCodeService;
        }

        // QR cho 1 OrderDetail
        [HttpGet("{orderDetailId}")]
        public async Task<IActionResult> GenerateQrCode(int orderDetailId)
        {
            var result = await _qrCodeService.GenerateQrCodeAsync(orderDetailId);
            if (result == null) return NotFound("OrderDetail not found");

            return Ok(result);
        }

        // QR cho nhiều OrderDetail
        [HttpPost("bulk")]
        public async Task<IActionResult> GenerateQrCodes([FromBody] List<int> orderDetailIds)
        {
            var results = await _qrCodeService.GenerateQrCodesAsync(orderDetailIds);
            return Ok(results);
        }
    }
}
