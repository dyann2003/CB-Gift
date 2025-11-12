using CB_Gift.DTOs;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CB_Gift.Controllers
{
    [ApiController]
    [Route("api/invoices")]
    [Authorize]
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoicesController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }
    // huy
        [HttpPost]
        [Authorize(Roles = "Staff,Manager")]
        public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceRequest request)
        {
            var staffId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var invoice = await _invoiceService.CreateInvoiceAsync(request, staffId);
            return Ok(invoice);
        }

        [HttpPost("create-payment-link")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> CreatePaymentLink([FromBody] CreatePaymentLinkRequest request)
        {
            try
            {
                var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var paymentLink = await _invoiceService.CreatePaymentLinkAsync(request, sellerId);
                return Ok(new { paymentUrl = paymentLink });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        /// <summary>
        /// Lấy danh sách hóa đơn cho Seller đang đăng nhập.
        /// Chỉ người dùng có vai trò "Seller" mới có thể gọi API này.
        /// </summary>
        // GET: /api/invoices/my-invoices
        [HttpGet("my-invoices")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetMyInvoices()
        {
            // Lấy ID của Seller từ token JWT một cách an toàn
            var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(sellerId))
            {
                return Unauthorized();
            }

            var invoices = await _invoiceService.GetInvoicesForSellerAsync(sellerId);
            return Ok(invoices);
        }
        [HttpGet("myinvoices")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetMyInvoices(
            [FromQuery] string? status = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(sellerId)) return Unauthorized();

            var result = await _invoiceService.GetInvoicesForSellerPageAsync(sellerId, status, searchTerm, page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một hóa đơn cụ thể bằng ID.
        /// Dành cho Staff, Admin xem hoặc Seller xem hóa đơn của chính mình.
        /// </summary>
        // GET: /api/invoices/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Staff, Manager, Seller")]
        public async Task<IActionResult> GetInvoiceDetails(int id)
        {
            var invoice = await _invoiceService.GetInvoiceDetailsAsync(id);

            if (invoice == null)
            {
                return NotFound(new { message = "Không tìm thấy hóa đơn." });
            }

            // Kiểm tra quyền truy cập
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Manager") && !User.IsInRole("Staff") && invoice.SellerUserId != currentUserId)
            {
                return Forbid(); // Trả về 403 Forbidden nếu không có quyền
            }

            return Ok(invoice);
        }
        /// <summary>
        /// [Staff/Admin] Lấy TẤT CẢ hóa đơn trong hệ thống (không phân trang).
        /// </summary>
        // GET: /api/invoices/all
        [HttpGet("all")]
        [Authorize(Roles = "Staff, Admin, Manager")]
        public async Task<IActionResult> GetAllInvoices(
            [FromQuery] string? status = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sellerId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _invoiceService.GetAllInvoicesAsync(status, searchTerm, sellerId, page, pageSize);
                return Ok(result); 
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi.", error = ex.Message }); 
            }
        }
        [HttpGet("seller-receivables")]
        [Authorize(Roles = "Staff, Manager, Admin")]
        public async Task<IActionResult> GetSellerReceivables(
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sortColumn = null,
            [FromQuery] string? sortDirection = "desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _invoiceService.GetSellerReceivablesAsync(searchTerm, sortColumn, sortDirection, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy dữ liệu công nợ.", error = ex.Message });
            }
        }
        // [CẬP NHẬT] - Endpoint cho tab "Payment History"
        [HttpGet("seller-payments/{sellerId}")]
        [Authorize(Roles = "Staff, Manager, Admin")]
        public async Task<IActionResult> GetPaymentsForSeller(string sellerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _invoiceService.GetPaymentsForSellerAsync(sellerId, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy lịch sử thanh toán.", error = ex.Message });
            }
        }

        // [THÊM MỚI] - API CHO TAB "SALES HISTORY" - LẤY CÁC THÁNG
        [HttpGet("seller-monthly-sales/{sellerId}")]
        [Authorize(Roles = "Staff, Manager, Admin")]
        public async Task<IActionResult> GetSellerMonthlySales(string sellerId)
        {
            try
            {
                var result = await _invoiceService.GetSellerMonthlySalesAsync(sellerId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy doanh số theo tháng.", error = ex.Message });
            }
        }

        // [THÊM MỚI] - API CHO TAB "SALES HISTORY" - LẤY ORDER TRONG THÁNG
        [HttpGet("seller-monthly-orders/{sellerId}")]
        [Authorize(Roles = "Staff, Manager, Admin")]
        public async Task<IActionResult> GetSellerOrdersForMonth(
            string sellerId,
            [FromQuery] int year,
            [FromQuery] int month,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 5)
        {
            try
            {
                var result = await _invoiceService.GetSellerOrdersForMonthAsync(sellerId, year, month, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy đơn hàng của tháng.", error = ex.Message });
            }
        }

        // [THÊM MỚI] - API CHO NÚT "CREATE MONTHLY RECEIPT"
        [HttpPost("create-monthly-invoice")]
        [Authorize(Roles = "Staff, Manager")]
        public async Task<IActionResult> CreateMonthlyInvoice([FromBody] CreateMonthlyInvoiceRequest request)
        {
            try
            {
                var staffId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var invoice = await _invoiceService.CreateInvoiceForMonthAsync(request, staffId);
                return Ok(invoice);
            }
            catch (InvalidOperationException ex)
            {
                // Lỗi nghiệp vụ (vd: không có order mới)
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo hóa đơn tháng.", error = ex.Message });
            }
        }
    }
}
