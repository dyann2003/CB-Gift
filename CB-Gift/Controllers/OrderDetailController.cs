using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CB_Gift.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderDetailController : ControllerBase
    {
        private readonly IOrderDetailService _orderDetailService;

        public OrderDetailController(IOrderDetailService orderDetailService)
        {
            _orderDetailService = orderDetailService;
        }

        [HttpGet("{orderDetailId}")]
        public async Task<IActionResult> GetOrderDetail(int orderDetailId)
        {
            var detail = await _orderDetailService.GetOrderDetailByIdAsync(orderDetailId);

            if (detail == null)
                return NotFound("OrderDetail not found");

            return Ok(detail);
        }

        [HttpPut("{orderDetailId}/accept")] // PUT api/OrderDetail/123/accept
        public async Task<IActionResult> AcceptOrderDetail(int orderDetailId)
        {
            var updatedOrderDetail = await _orderDetailService.AcceptOrderDetailAsync(orderDetailId);

            if (updatedOrderDetail == null)
            {
                return NotFound($"Không tìm thấy OrderDetail với ID: {orderDetailId}");
            }

            return NoContent();
        }

        [HttpPut("{orderDetailId}/reject")] // PUT api/OrderDetail/123/reject
        public async Task<IActionResult> RejectOrderDetail(int orderDetailId)
        {
            var updatedOrderDetail = await _orderDetailService.RejectOrderDetailAsync(orderDetailId);

            if (updatedOrderDetail == null)
            {
                return NotFound($"Không tìm thấy OrderDetail với ID: {orderDetailId}");
            }

            return NoContent();
        }

    }
}
