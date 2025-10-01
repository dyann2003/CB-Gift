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

    }
}
