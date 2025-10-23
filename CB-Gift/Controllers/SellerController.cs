using CB_Gift.DTOs;
using CB_Gift.Models.Enums;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CB_Gift.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Seller,Admin")]
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

        [HttpGet]
        public async Task<IActionResult> GetMyOrders()
        {
            string sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var orders = await _orderService.GetOrdersAndOrderDetailForSellerAsync(sellerId);
            return Ok(orders);
        }
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
                // Gọi Service (đã thêm kiểm tra Variant ID)
                await _orderService.AddOrderDetailAsync(orderId, request, sellerId);
                return Ok(new { message = "Order detail added successfully." });
            }
            catch (ArgumentException ex)
            {
                // Bắt lỗi khi ProductVariantID không tồn tại
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                // Bắt các lỗi chung khác
                return StatusCode(500, new { error = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        // Endpoint để giao việc
        // POST: /api/seller/tasks/order-details/123/assign
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
        //assign theo OrderId
        // POST: /api/seller/orders/{orderId}/assign-designer
        [HttpPost("orders/{orderId}/assign-designer")]
        public async Task<IActionResult> AssignDesignerToOrder(int orderId, [FromBody] AssignDesignerToOrderDetailDto dto)
        {
            try
            {
                var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(sellerId)) return Unauthorized();

                // Gọi phương thức service mới
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
                // _logger.LogError(...)
                return StatusCode(500, new { message = "Đã xảy ra lỗi không mong muốn." });
            }
        }
        //Get Designer của seller
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
            catch (Exception ex)
            {
                // Ghi lại log lỗi chi tiết ở phía server để bạn có thể điều tra
                // _logger.LogError(ex, "Lỗi xảy ra khi Seller {SellerId} lấy danh sách designer.", sellerId);

                // Chỉ trả về một thông báo lỗi chung chung cho client
                return StatusCode(500, new { message = "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau." });
            }
        }
        [HttpPut]
        public async Task<IActionResult> UpdateDesignStatus(
        int orderId,
        [FromBody] UpdateStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // LẤY SELLER ID DƯỚI DẠNG STRING
            var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(sellerId))
            {
                return Unauthorized("User is not authenticated or Seller ID missing.");
            }

            var action = request.ProductionStatus;

            // Kiểm tra tính hợp lệ của Action (phải là DESIGN_REDO (5) hoặc READY_PROD (6))
            if (action != ProductionStatus.DESIGN_REDO && action != ProductionStatus.READY_PROD)
            {
                return BadRequest($"Invalid action. Must be {ProductionStatus.DESIGN_REDO} (DESIGN_REDO) or {ProductionStatus.READY_PROD} (READY_PROD).");
            }

            try
            {
                // GỌI SERVICE VỚI SELLER ID LÀ STRING
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
    }
}
