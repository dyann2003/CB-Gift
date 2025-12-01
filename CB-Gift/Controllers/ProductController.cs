using CB_Gift.DTOs;
using CB_Gift.Services;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CB_Gift.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _service;
        private readonly ITagService _tagService;

        public ProductController(IProductService service, ITagService tagService)
        {
            _service = service;
            _tagService = tagService;
        }

        // GET: api/Product
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var products = await _service.GetAllAsync();
            return Ok(products);
        }

        [HttpGet("filter")]
        [AllowAnonymous]
        public async Task<IActionResult> FilterProducts(
           string? searchTerm = "",
           string? category = "",
           int? status = null,
           int page = 1,
           int pageSize = 10)
        {
            var (total, products) = await _service.FilterProductsAsync(
                searchTerm, category, status, page, pageSize);

            return Ok(new { total, products });
        }

        //GET: api/Prodcut/active   -- các sản phẩm được active
        [HttpGet("active")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetAllProductHaveStatusTrue()
        {
            var products = await _service.GetAllProductsHaveStatusTrueAsync();
            return Ok(products);
        }
        //GET: api/Product/hidden  -- Các sản phẩm bị ẩn
        [HttpGet("hidden")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetHiddenProducts()
        {
            var products = await _service.GetHiddenProductsAsync();
            return Ok(products);
        }

        // GET: api/Product/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _service.GetByIdAsync(id);
            if (product == null)
                return NotFound();

            return Ok(product);
        }

        // POST: api/Product
        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create([FromBody] ProductCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.ProductId }, created);
        }

        // PUT: api/Product/5 //Update Product
        [HttpPut("{id}")]
        [Authorize(Roles = "Manager")]
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
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }



        [HttpDelete("hidden/{id}")]
        [Authorize(Roles = "Manager")]
        // Ẩn sản phẩm
        public async Task<IActionResult> SoftDeleteProduct(int id)
        {
            var sucess = await _service.SoftDeleteProductAsync(id);
            if(!sucess) return NotFound();
            return Ok(new {message = "Product hidden successfully!"});
        }
        //Hiển thị lại sản phẩm
        [HttpPatch("restore/{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> RestoreProduct(int id)
        {
            var sucess = await _service.RestoreProductAsync(id);
            if (!sucess) return NotFound();
            return Ok(new { message = "Product restored successfully!" });
        }

        //Update Status Product theo danh sách
        [HttpPut("bulk-update-status")]
        [Authorize(Roles = "Manager")]
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

        // --- API QUẢN LÝ TAG CHO PRODUCT ---

        // GET: api/Products/1/tags
        [HttpGet("{productId}/tags")]
        public async Task<IActionResult> GetProductTags(int productId)
        {
            var tags = await _tagService.GetTagsByProductIdAsync(productId);
            if (tags == null) return NotFound("Sản phẩm không tồn tại.");
            return Ok(tags);
        }

        // POST: api/Products/1/tags/5  (Gán tag 5 cho sản phẩm 1)
        [HttpPost("{productId}/tags/{tagId}")]
        public async Task<IActionResult> AddTagToProduct(int productId, int tagId)
        {
            var success = await _tagService.AddTagToProductAsync(productId, tagId);
            if (!success) return NotFound("Sản phẩm hoặc Tag không tồn tại.");
            return Ok();
        }

        // DELETE: api/Products/1/tags/5 (Gỡ tag 5 khỏi sản phẩm 1)
        [HttpDelete("{productId}/tags/{tagId}")]
        public async Task<IActionResult> RemoveTagFromProduct(int productId, int tagId)
        {
            var success = await _tagService.RemoveTagFromProductAsync(productId, tagId);
            if (!success) return NotFound("Sản phẩm không tồn tại.");
            return NoContent();
        }

    }
}
