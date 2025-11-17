using CB_Gift.DTOs;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CB_Gift.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize(Roles = "Manager")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // ---------------------------
        // 🔍 FILTER + SEARCH + PAGING
        // ---------------------------
        [HttpGet("filter")]
        public async Task<IActionResult> GetFilteredCategories(
            [FromQuery] string? searchTerm = null,
            [FromQuery] int? status = null,
            [FromQuery] string? sortColumn = null,
            [FromQuery] string? sortDirection = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var (categories, total) = await _categoryService.GetFilteredAndPagedCategoriesAsync(
                    searchTerm, status, sortColumn, sortDirection, page, pageSize);

                return Ok(new { total, categories });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi lấy danh sách danh mục.",
                    detail = ex.Message
                });
            }
        }

        // ---------------------------
        // GET PUBLIC ACTIVE CATEGORIES
        // ---------------------------
        [HttpGet("public")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicCategories()
        {
            var categories = await _categoryService.GetActiveCategoriesAsync();
            return Ok(categories);
        }

        // ---------------------------
        // GET CATEGORY BY ID
        // ---------------------------
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
                return NotFound($"Không tìm thấy danh mục với ID = {id}.");

            return Ok(category);
        }

        // ---------------------------
        // CREATE CATEGORY
        // ---------------------------
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto createCategoryDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var newCategory = await _categoryService.CreateCategoryAsync(createCategoryDto);
                return CreatedAtAction(nameof(GetCategory), new { id = newCategory.CategoryId }, newCategory);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ---------------------------
        // UPDATE CATEGORY (ONLY name + code)
        // ---------------------------
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDto updateCategoryDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var success = await _categoryService.UpdateCategoryAsync(id, updateCategoryDto);

                if (!success)
                    return NotFound($"Không tìm thấy danh mục với ID = {id}.");

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ---------------------------
        // SOFT DELETE CATEGORY (status = 0)
        // ---------------------------
        [HttpDelete("{id}")]
        public async Task<IActionResult> SoftDeleteCategory(int id)
        {
            try
            {
                var success = await _categoryService.UpdateCategoryStatusAsync(
                    id,
                    new UpdateCategoryStatusDto { Status = 0 }
                );

                if (!success)
                    return NotFound($"Không tìm thấy danh mục với ID = {id}.");

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ---------------------------
        // UPDATE STATUS (Restore, Disable…)
        // ---------------------------
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateCategoryStatus(int id, [FromBody] UpdateCategoryStatusDto statusDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _categoryService.UpdateCategoryStatusAsync(id, statusDto);

            if (!success)
                return NotFound($"Không tìm thấy danh mục với ID = {id}.");

            return NoContent();
        }
    }
}
