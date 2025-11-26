using CB_Gift.DTOs.Reports;
using CB_Gift.Services.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CB_Gift.Controllers
{
    [Route("api/reports/operations")] // Route riêng cho operations
    [ApiController]
    [Authorize(Roles = "Manager,Admin,QC")]
    public class OperationsReportsController : ControllerBase
    {
        private readonly IReportService _service;
        public OperationsReportsController(IReportService service) { _service = service; }

        [HttpGet("kpis")]
        public async Task<IActionResult> GetKpis([FromQuery] ReportFilterDto filter)
            => Ok(await _service.GetOperationsKpisAsync(filter));

        [HttpGet("status-chart")]
        public async Task<IActionResult> GetStatusChart([FromQuery] ReportFilterDto filter)
            => Ok(await _service.GetOrderStatusDistributionAsync(filter));

        [HttpGet("incoming-outgoing")]
        public async Task<IActionResult> GetIncomingOutgoing([FromQuery] ReportFilterDto filter)
            => Ok(await _service.GetIncomingOutgoingChartAsync(filter));

        [HttpGet("issues-breakdown")]
        public async Task<IActionResult> GetIssues([FromQuery] ReportFilterDto filter)
            => Ok(await _service.GetIssueBreakdownAsync(filter));

        [HttpGet("critical-alerts")]
        public async Task<IActionResult> GetCriticals([FromQuery] ReportFilterDto filter)
            => Ok(await _service.GetCriticalOrdersAsync(filter));
    }
}
