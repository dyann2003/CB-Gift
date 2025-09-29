using Microsoft.AspNetCore.Mvc;

namespace CB_Gift.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SellerController : ControllerBase
    {
        [HttpGet("index")]
        public IActionResult Index()
        {
            return Ok(new { message = "This is seller index API" });
        }
    }
}
