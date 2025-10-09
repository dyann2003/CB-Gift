using CB_Gift.DTOs;
using CB_Gift.Services;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Mvc;

namespace CB_Gift.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _service;

        public ProductController(IProductService service)
        {
            _service = service;
        }

        // GET: api/Product
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _service.GetAllAsync();
            return Ok(products);
        }
        //GET: api/Prodcut/active   -- các sản phẩm được active
        [HttpGet("active")]
        public async Task<IActionResult> GetAllProductHaveStatusTrue()
        {
            var products = await _service.GetAllProductsHaveStatusTrueAsync();
            return Ok(products);
        }
        //GET: api/Product/hidden  -- Các sản phẩm bị ẩn
        [HttpGet("hidden")]
        public async Task<IActionResult> GetHiddenProducts()
        {
            var products = await _service.GetHiddenProductsAsync();
            return Ok(products);
        }

        // GET: api/Product/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _service.GetByIdAsync(id);
            if (product == null)
                return NotFound();

            return Ok(product);
        }

        // POST: api/Product
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.ProductId }, created);
        }

        // PUT: api/Product/5 //Update Product
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _service.UpdateAsync(id, dto);
            if (updated == null)
                return NotFound();

            return Ok(updated);
        }

        // DELETE: api/Product/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }
        [HttpDelete("hidden/{id}")]
        // Ẩn sản phẩm
        public async Task<IActionResult> SoftDeleteProduct(int id)
        {
            var sucess = await _service.SoftDeleteProductAsync(id);
            if(!sucess) return NotFound();
            return Ok(new {message = "Product hidden successfully!"});
        }
        //Hiển thị lại sản phẩm
        [HttpPatch("restore/{id}")]
        public async Task<IActionResult> RestoreProduct(int id)
        {
            var sucess = await _service.RestoreProductAsync(id);
            if (!sucess) return NotFound();
            return Ok(new { message = "Product restored successfully!" });
        }

        //Update Status Product theo danh sách
        [HttpPut("bulk-update-status")]
        public async Task<IActionResult> BulkUpdateStatus([FromBody] BulkUpdateProductStatusDto request)
        {
            try
            {
                var userName = User.Identity?.Name ?? "System";

                var (count, updated) = await _service.BulkUpdateStatusAsync(request, userName);

                return Ok(new
                {
                    message = $"Đã cập nhật trạng thái {count} sản phẩm và cập nhật bởi {userName}.",
                    updated
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

    }
}
