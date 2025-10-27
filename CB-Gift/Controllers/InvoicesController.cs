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
        public async Task<IActionResult> GetAllInvoices()
        {
            try
            {
                var invoices = await _invoiceService.GetAllInvoicesAsync();
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi không mong muốn.", error = ex.Message });
            }
        }
    }
}
