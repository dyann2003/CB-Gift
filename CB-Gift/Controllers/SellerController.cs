using CB_Gift.DTOs;
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


        public SellerController(IOrderService orderService)
        {
            _orderService = orderService;
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


        [HttpPost("{id}/details")]
        public async Task<IActionResult> AddOrderDetail(int id, [FromBody] OrderDetailCreateRequest request)
        {
            string sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _orderService.AddOrderDetailAsync(id, request, sellerId);
            return Ok(new { message = "Order detail added." });
        }
    }
}
