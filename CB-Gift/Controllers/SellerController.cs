using Microsoft.AspNetCore.Mvc;

namespace CB_Gift.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SellerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
