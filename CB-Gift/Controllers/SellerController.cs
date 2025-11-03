// Trong SellerController.cs

using CB_Gift.DTOs;
using CB_Gift.Models.Enums;
using CB_Gift.Services;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CB_Gift.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Seller")]
    public class SellerController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IDesignerTaskService _designerTaskService;
        private readonly IDesignerSellerService _designerSellerservice;


        public SellerController(IOrderService orderService, IDesignerTaskService designerTaskService, IDesignerSellerService designerSellerservice)
        {
            _orderService = orderService;
            _designerTaskService = designerTaskService;
            _designerSellerservice = designerSellerservice;
        }

        // ----------------------------------------------------------------------
        // ✅ API ĐÃ SỬA: GET ORDERS (Dùng Service tối ưu)
        // ----------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetMyOrders(
            [FromQuery] string? status = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sortColumn = null,
            [FromQuery] string? sortDirection = "desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            string sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            try
            {
                var (orders, total) = await _orderService.GetFilteredAndPagedOrdersForSellerAsync(
                    sellerId, status, searchTerm, sortColumn, sortDirection, page, pageSize);

                return Ok(new { total, orders });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy danh sách đơn hàng.", detail = ex.Message });
            }
        }

        // ----------------------------------------------------------------------
        // ✅ API MỚI: GET DASHBOARD STATS (Dùng Service tối ưu)
        // ----------------------------------------------------------------------
        [HttpGet("DashboardStats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var stats = await _orderService.GetDashboardStatsForSellerAsync(userId);

            return Ok(stats);
        }

        // ----------------------------------------------------------------------
        // Giữ nguyên các API còn lại
        // ----------------------------------------------------------------------

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateRequest request)
        {
            string sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var orderId = await _orderService.CreateOrderAsync(request, sellerId);
            return Ok(new { message = "Order created successfully.", orderId });
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderDetail(int id)
        {
            string sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _orderService.GetOrderDetailAsync(id, sellerId);
            return result == null ? NotFound() : Ok(result);
        }


        [HttpPost("{orderId}/details")]
        public async Task<IActionResult> AddOrderDetail(int orderId, [FromBody] OrderDetailCreateRequest request)
        {
            string sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            try
            {
                await _orderService.AddOrderDetailAsync(orderId, request, sellerId);
                return Ok(new { message = "Order detail added successfully." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        [HttpPost("order-details/{orderDetailId}/assign")]
        public async Task<IActionResult> AssignDesigner(int orderDetailId, [FromBody] AssignDesignerToOrderDetailDto dto)
        {
            var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(sellerId)) return Unauthorized();

            try
            {
                var success = await _designerTaskService.AssignDesignerToOrderDetailAsync(orderDetailId, dto.DesignerUserId, sellerId);

                if (!success)
                {
                    return NotFound("Không tìm thấy Chi tiết đơn hàng hoặc bạn không có quyền truy cập.");
                }

                return Ok(new { message = "Giao việc cho Designer thành công. Đơn hàng đã chuyển sang trạng thái Cần Design." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, "Đã xảy ra lỗi không mong muốn.");
            }
        }

        [HttpPost("orders/{orderId}/assign-designer")]
        public async Task<IActionResult> AssignDesignerToOrder(int orderId, [FromBody] AssignDesignerToOrderDetailDto dto)
        {
            try
            {
                var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(sellerId)) return Unauthorized();

                var success = await _designerTaskService.AssignDesignerToOrderAsync(orderId, dto.DesignerUserId, sellerId);

                if (!success)
                {
                    return NotFound(new { message = "Không tìm thấy Đơn hàng hoặc bạn không có quyền truy cập." });
                }

                return Ok(new { message = "Giao việc cho Designer thành công. Toàn bộ đơn hàng đã được cập nhật." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi không mong muốn." });
            }
        }

        [HttpGet("my-designer")]
        public async Task<IActionResult> GetDesignersForSeller()
        {
            try
            {
                var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(sellerId))
                {
                    return Unauthorized(new { message = "Không thể xác định người dùng. Vui lòng đăng nhập lại." });
                }

                var result = await _designerSellerservice.GetDesignersForSellerAsync(sellerId);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau." });
            }
        }
        [HttpPut("orders/{orderId}/approve-or-reject-design")]
        public async Task<IActionResult> UpdateDesignStatus(
        int orderId,
        [FromBody] UpdateStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(sellerId))
            {
                return Unauthorized("User is not authenticated or Seller ID missing.");
            }

            var action = request.ProductionStatus;

            if (action != ProductionStatus.DESIGN_REDO && action != ProductionStatus.READY_PROD)
            {
                return BadRequest($"Invalid action. Must be {ProductionStatus.DESIGN_REDO} (DESIGN_REDO) or {ProductionStatus.READY_PROD} (READY_PROD).");
            }

            try
            {
                bool success = await _orderService.SellerApproveOrderDesignAsync(
                    orderId,
                    action,
                    sellerId
                );

                if (success)
                {
                    var actionText = action == ProductionStatus.READY_PROD
                                        ? "CONFIRMED (Chốt Đơn)"
                                        : "DESIGN_REDO (Thiết Kế Lại)";

                    return Ok(new { message = $"Order {orderId} design successfully updated to {actionText}." });
                }
                else
                {
                    return NotFound($"Order with ID {orderId} not found.");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal server error during status update.");
            }
        }
        [HttpPut("order/order-details/{orderDetailId}/design-status")]
        public async Task<IActionResult> UpdateDesignOrderDetailStatus(
    int orderDetailId,
    [FromBody] UpdateStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(sellerId))
            {
                return Unauthorized("User is not authenticated or Seller ID missing.");
            }

            var action = request.ProductionStatus;

            if (action != ProductionStatus.DESIGN_REDO && action != ProductionStatus.READY_PROD)
            {
                return BadRequest($"Invalid action. Must be {ProductionStatus.DESIGN_REDO} or {ProductionStatus.READY_PROD}.");
            }

            // Nếu hành động là "Làm lại" (DESIGN_REDO), thì BẮT BUỘC phải có lý do
            if (action == ProductionStatus.DESIGN_REDO && string.IsNullOrWhiteSpace(request.Reason))
            {
                return BadRequest(new { message = "A reason is required when rejecting a design (DESIGN_REDO)." });
            }

            try
            {
                bool success = await _orderService.SellerApproveOrderDetailDesignAsync(
                    orderDetailId,
                    action,
                    sellerId,
                    request.Reason 
                );

                if (success)
                {
                    return Ok(new { message = $"OrderDetail {orderDetailId} design successfully updated to {action}." });
                }
                else
                {
                    return NotFound($"OrderDetail with ID {orderDetailId} not found.");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal server error during status update.");
            }
        }
        [HttpPost("orders/{orderId}/send-to-staff")]
        public async Task<IActionResult> SendOrderToReadyProd(int orderId)
        {
            var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(sellerId)) return Unauthorized();

            try
            {
                var success = await _orderService.SendOrderToReadyProdAsync(orderId, sellerId);

                if (!success)
                {
                    return NotFound(new { message = $"Không tìm thấy Đơn hàng ID {orderId} hoặc bạn không có quyền truy cập." });
                }

                return Ok(new { message = "Đơn hàng đã được chốt và chuyển thẳng sang trạng thái Sẵn sàng Sản xuất (READY_PROD)." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi không mong muốn khi gửi đơn hàng." });
            }
        }
    }
}