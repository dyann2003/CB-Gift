using AutoMapper;
using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Models;
using CB_Gift.Services.IService;
using Microsoft.EntityFrameworkCore;

namespace CB_Gift.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly CBGiftDbContext _context; 
        private readonly IMapper _mapper;

        public CategoryService(CBGiftDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
        {
            var categories = await _context.Categories.AsNoTracking().ToListAsync();
            return _mapper.Map<IEnumerable<CategoryDto>>(categories);
        }

        public async Task<CategoryDto> GetCategoryByIdAsync(int id)
        {
            var category = await _context.Categories.AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryId == id);
            return _mapper.Map<CategoryDto>(category);
        }
        public async Task<IEnumerable<CategoryDto>> GetActiveCategoriesAsync()
        {
            var categories = await _context.Categories
                .Where(c => c.Status == 1) // Chỉ lấy danh mục đang hoạt động
                .AsNoTracking()
                .ToListAsync();
            return _mapper.Map<IEnumerable<CategoryDto>>(categories);
        }
        public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createCategoryDto)
        {
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryCode.ToLower() == createCategoryDto.CategoryCode.ToLower());

            if (existingCategory != null)
            {
                throw new Exception("Mã danh mục này đã tồn tại.");
            }

            var category = _mapper.Map<Category>(createCategoryDto);
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return _mapper.Map<CategoryDto>(category);
        }

        public async Task<bool> UpdateCategoryAsync(int id, UpdateCategoryDto updateCategoryDto)
        {
            var categoryToUpdate = await _context.Categories.FindAsync(id);
            if (categoryToUpdate == null)
            {
                return false; // Không tìm thấy để cập nhật
            }

            // Map các giá trị từ DTO vào entity đã tồn tại
            _mapper.Map(updateCategoryDto, categoryToUpdate);
            _context.Entry(categoryToUpdate).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var categoryToDelete = await _context.Categories.FindAsync(id);
            if (categoryToDelete == null)
            {
                return false; // Không tìm thấy để xóa
            }

            // Kiểm tra ràng buộc: không xóa danh mục nếu có sản phẩm liên quan
            bool hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id); // Giả sử Product có khóa ngoại CategoryId
            if (hasProducts)
            {
                throw new Exception("Không thể xóa danh mục đã có sản phẩm.");
            }

            _context.Categories.Remove(categoryToDelete);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> UpdateCategoryStatusAsync(int id, UpdateCategoryStatusDto statusDto)
        {
            var categoryToUpdate = await _context.Categories.FindAsync(id);
            if (categoryToUpdate == null)
            {
                return false; // Không tìm thấy
            }

            categoryToUpdate.Status = statusDto.Status;
            _context.Entry(categoryToUpdate).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
