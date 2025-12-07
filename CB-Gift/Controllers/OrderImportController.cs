using CB_Gift.Orders.Import;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CB_Gift.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderImportController : ControllerBase
    {
        private readonly IOrderImportService _importService;

        public OrderImportController(IOrderImportService importService)
        {
            _importService = importService;
        }

        [HttpPost("import")]
        [Authorize]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("File không hợp lệ.");
            var sellerUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(sellerUserId)) return Unauthorized("Không xác định Seller.");


            var result = await _importService.ImportFromExcelAsync(file, sellerUserId);


            string? base64 = null;
            if (result.ErrorReportFileBytes != null && result.ErrorReportFileBytes.Length > 0)
            {
                base64 = Convert.ToBase64String(result.ErrorReportFileBytes);
            }


            return Ok(new
            {
                result.TotalRows,
                result.SuccessCount,
                Errors = result.Errors,
                ErrorReportFileName = result.ErrorReportFileName,
                ErrorReportBase64 = base64
            });
        }

        [HttpGet("template")]
        [Authorize]
        public IActionResult Template()
        {
            var svc = HttpContext.RequestServices.GetService(typeof(IOrderImportService)) as IOrderImportService;
            if (svc == null) return StatusCode(StatusCodes.Status500InternalServerError);
            var bytes = svc.GenerateExcelTemplate();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "order-import-template.xlsx");
        }
    }
}
