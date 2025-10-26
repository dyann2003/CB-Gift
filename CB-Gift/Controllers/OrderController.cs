using CB_Gift.DTOs;
using CB_Gift.Services.IService;
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
        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet("GetAllOrders")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _orderService.GetAllOrders();
            return Ok(orders);
        }

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
    }
}
