using CB_Gift.DTOs;
using CB_Gift.Services;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CB_Gift.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ICancellationService _cancellationService;
        private readonly IRefundService _refundService;
        public OrderController(IOrderService orderService, ICancellationService cancellationService, IRefundService refundService)
        {
            _orderService = orderService;
            _cancellationService = cancellationService;
            _refundService = refundService;
        }

        [HttpGet("GetAllOrders")]
        [Authorize(Roles = "Staff,Manager,QC")]
        public async Task<IActionResult> GetAllOrders(
     [FromQuery] string? status = null,
     [FromQuery] string? searchTerm = null,
     [FromQuery] string? sortColumn = null,
     [FromQuery] string? sellerId=null,
     [FromQuery] string? sortDirection = "desc",
     [FromQuery] DateTime? fromDate = null,
     [FromQuery] DateTime? toDate = null,
     [FromQuery] int page = 1,
     [FromQuery] int pageSize = 10)
        {
            try
            {
                var (orders, total) = await _orderService.GetFilteredAndPagedOrdersAsync(
                    status, searchTerm, sortColumn, sellerId, sortDirection, fromDate, toDate, page, pageSize);

                return Ok(new { total, orders });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Đã xảy ra lỗi khi lấy danh sách đơn hàng.",
                    detail = ex.Message
                });
            }
        }





        //[HttpGet("GetAllOrders")]
        //public async Task<IActionResult> GetAllOrders()
        //{
        //    var orders = await _orderService.GetAllOrders();
        //    return Ok(orders);
        //}


        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderWithDetails(int orderId)
        {
            var order = await _orderService.GetOrderDetailAsync(orderId, null);
            if (order == null) return NotFound("Order not found");

            return Ok(order);
        }
        //Step1: Tao khach hang ( EndCustomer) => return EndCustomer
        [HttpPost("CreateEndCustomer")]
        public async Task<IActionResult> CreateEndCustomer(EndCustomerCreateRequest request)
        {
            var result = await _orderService.CreateCustomerAsync(request);
            return Ok(result);
        }
        //lay SellerUserID để lưu vào Order
        private string GetSellerUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new Exception("SellerUserId not found.");
        }

        //Step2: Tao Order => return Order
        [HttpPost("CreateOrder")]
        public async Task<IActionResult> CreateOrder(OrderCreateRequest request)
        {
            var sellerUserId = GetSellerUserId();
            var orderId = await _orderService.CreateOrderAsync(request,sellerUserId);
            return Ok(new { OrderId = orderId, Message = "Order created successfully" });
        }

        //Step3: Tao OrderDetail
        [HttpPost("{orderId}/AddOrderDetail")]
        public async Task<IActionResult> AddOrderDetail(int orderId,OrderDetailCreateRequest request)
        {
            var sellerUserId = GetSellerUserId();
            await _orderService.AddOrderDetailAsync(orderId, request,sellerUserId);
            return Ok(new { Message = "Order detail added successfully" });
            
        }
        // Make Order Tổng
        [HttpPost("make-order")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> MakeOrder([FromBody] MakeOrderDto request)
        {
            try
            {
                var sellerUserId = GetSellerUserId();
                await _orderService.MakeOrder(request, sellerUserId);
                return Ok(new { message = "Order created susscessfully!" });
            }catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            
        }
        //update order
        // Trong SellerController.cs

        [HttpPut("update-order/{id}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] OrderUpdateDto request)
        {
            try
            {
                string sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var response = await _orderService.UpdateOrderAsync(id, request, sellerId);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi không mong muốn.", error = ex.Message });
            }
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            try
            {
                string sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var success = await _orderService.DeleteOrderAsync(id, sellerId);

                if (!success)
                {
                    return NotFound(new { message = "Không tìm thấy đơn hàng hoặc bạn không có quyền truy cập." });
                }

                return Ok(new { message = "Xóa đơn hàng và các chi tiết liên quan thành công." });
            }
            catch (InvalidOperationException ex)
            {
               // _logger.LogWarning(ex, "Delete Order failed for Order ID {OrderId}: Invalid State.", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
               // _logger.LogError(ex, "An unexpected error occurred while deleting Order ID {OrderId}.", id);
                return StatusCode(500, new { message = "Đã xảy ra lỗi không mong muốn." });
            }
        }

        [HttpPut("{orderId}/approve-shipping")]
        public async Task<IActionResult> ApproveOrderForShipping([FromRoute] int orderId)
        {
            if (orderId <= 0)
            {
                return BadRequest("Invalid Order ID.");
            }

            try
            {
                var result = await _orderService.ApproveOrderForShippingAsync(orderId);

                if (!result.IsSuccess)
                {
                    if (!result.OrderFound)
                    {
                        return NotFound($"Order with ID {orderId} not found.");
                    }
                    if (!result.CanApprove)
                    {
                        return BadRequest(result.ErrorMessage ?? "Order cannot be approved. Check product statuses.");
                    }
                    return BadRequest(result.ErrorMessage ?? "Failed to approve order.");
                }

                return Ok(new { message = $"Order {orderId} approved for shipping." });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while approving the order.");
            }
        }
        /// <summary>
        /// (SELLER) Gửi yêu cầu hủy một đơn hàng
        /// </summary>
        [HttpPost("{id}/request-cancellation")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> RequestOrderCancellation(int id, [FromBody] CancelRequestDto request)
        {
            try
            {
                var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _cancellationService.RequestCancellationAsync(id, request, sellerId);
                return Ok(new { message = "Đã gửi yêu cầu hủy đơn hàng." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống.", error = ex.Message });
            }
        }

        /// <summary>
        /// (STAFF/MANAGER) Phê duyệt hoặc từ chối một yêu cầu hủy đơn hàng
        /// </summary>
        [HttpPost("{id}/review-cancellation")]
        [Authorize(Roles = "Staff,Manager")]
        public async Task<IActionResult> ReviewOrderCancellation(int id, [FromBody] ReviewCancelRequestDto request)
        {
            try
            {
                var staffId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _cancellationService.ReviewCancellationAsync(id, request, staffId);

                string message = request.Approved
                    ? "Đã CHẤP NHẬN hủy đơn hàng."
                    : "Đã TỪ CHỐI yêu cầu hủy đơn hàng.";

                return Ok(new { message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex) // Bắt lỗi thiếu lý do từ chối
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống.", error = ex.Message });
            }
        }
        [HttpPost("{id}/request-refund")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> RequestOrderRefund(int id, [FromBody] SellerRefundRequestDto request)
        {
            try
            {
                var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _refundService.RequestRefundAsync(id, request, sellerId);
                return Ok(new { message = "Đã gửi yêu cầu hoàn tiền thành công. Chờ Staff xem xét." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống.", error = ex.Message });
            }
        }

        // (STAFF/MANAGER) PHÊ DUYỆT HOẶC TỪ CHỐI YÊU CẦU HOÀN TIỀN ===
        // Lưu ý: Route này trỏ đến ID của Yêu cầu (RefundRequest), không phải ID của Order
        [HttpPost("refund-requests/{refundId}/review")]
        [Authorize(Roles = "Staff,Manager")]
        public async Task<IActionResult> ReviewRefund(int refundId, [FromBody] StaffReviewRefundDto request)
        {
            try
            {
                var staffId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _refundService.ReviewRefundAsync(refundId, request, staffId);

                string message = request.Approved
                    ? "Đã CHẤP NHẬN yêu cầu hoàn tiền."
                    : "Đã TỪ CHỐI yêu cầu hoàn tiền.";

                return Ok(new { message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex) // Bắt lỗi thiếu lý do từ chối
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống.", error = ex.Message });
            }
        }
        [HttpGet("GetAllOrdersForInvoice")]
        [Authorize(Roles = "Staff,Manager,QC")]
        public async Task<IActionResult> GetAllOrdersForInvoice(
            [FromQuery] string? status = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? seller = null,
            [FromQuery] string? sortColumn = null,
            [FromQuery] string? sortDirection = "desc",
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var (orders, total) = await _orderService.GetFilteredAndPagedOrdersForInvoiceAsync(
                    status, searchTerm, seller, sortColumn, sortDirection, fromDate, toDate, page, pageSize);

                return Ok(new { total, orders });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Đã xảy ra lỗi khi lấy danh sách đơn hàng.",
                    detail = ex.Message
                });
            }
        }

        [HttpGet("GetUniqueSellers")]
        public async Task<IActionResult> GetUniqueSellers([FromQuery] string? status = null)
        {
            try
            {
                var sellerNames = await _orderService.GetUniqueSellersAsync(status);
                return Ok(sellerNames);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Đã xảy ra lỗi khi lấy danh sách sellers.",
                    detail = ex.Message
                });
            }
        }
        [HttpGet("manager/{id}")]
        [Authorize(Roles = "Staff,Manager,QC")] // Chỉ cho phép nội bộ truy cập
        public async Task<IActionResult> GetManagerOrderDetail(int id)
        {
            try
            {
                // Gọi hàm service tách riêng mà chúng ta vừa viết
                var orderDetail = await _orderService.GetManagerOrderDetailAsync(id);

                if (orderDetail == null)
                {
                    return NotFound(new { message = $"Order with ID {id} not found." });
                }

                return Ok(orderDetail);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server khi lấy chi tiết đơn hàng.", error = ex.Message });
            }
        }
        /*[HttpPost("import")]

        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File không hợp lệ.");

            var sellerUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(sellerUserId))
                return Unauthorized("Không xác định được Seller hiện tại.");

            var result = await _orderService.ImportFromExcelAsync(file, sellerUserId);

            return Ok(result);
        }*/

        [HttpPost("import")]

        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File không hợp lệ.");

            var sellerUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(sellerUserId))
                return Unauthorized("Không xác định được Seller hiện tại.");

            var result = await _orderService.ImportFromExcelAsync(file, sellerUserId);

            return Ok(result);
        }
    }
}
