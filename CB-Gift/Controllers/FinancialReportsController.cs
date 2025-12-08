using CB_Gift.DTOs.Reports;
using CB_Gift.Services.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CB_Gift.Controllers
{
    [Route("api/reports/financial")]
    [ApiController]
    [Authorize(Roles = "Manager,Admin")]
    public class FinancialReportsController : ControllerBase
    {
        private readonly IReportService _service;
        public FinancialReportsController(IReportService service) { _service = service; }

        [HttpGet("kpis")]
        public async Task<IActionResult> GetKpis([FromQuery] ReportFilterDto filter) => Ok(await _service.GetFinancialKpisAsync(filter));

        [HttpGet("revenue-chart")]
        public async Task<IActionResult> GetRevenueChart([FromQuery] ReportFilterDto filter) => Ok(await _service.GetRevenueChartAsync(filter));

        [HttpGet("issues-chart")]
        public async Task<IActionResult> GetIssuesChart([FromQuery] ReportFilterDto filter) => Ok(await _service.GetFinancialIssuesChartAsync(filter));

        [HttpGet("reasons-chart")]
        public async Task<IActionResult> GetReasonsChart([FromQuery] ReportFilterDto filter) => Ok(await _service.GetReprintReasonsChartAsync(filter));

        [HttpGet("top-sellers")]
        public async Task<IActionResult> GetTopSellers([FromQuery] ReportFilterDto filter) => Ok(await _service.GetTopSellersAsync(filter));
        [Authorize(Roles = "Staff,Manager,QC")]
        [HttpGet("all-sellers")]
        public async Task<IActionResult> GetAllSellers()
        {
            try
            {
                var sellers = await _service.GetAllSellersAsync();
                return Ok(sellers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi không mong muốn khi lấy danh sách seller.", error = ex.Message });
            }
        }
    }
}
