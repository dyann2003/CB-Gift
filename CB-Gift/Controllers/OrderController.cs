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

    }
}
