using CB_Gift.DTOs;

namespace CB_Gift.Services.IService
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
        Task<IEnumerable<CategoryDto>> GetActiveCategoriesAsync();
        Task<CategoryDto> GetCategoryByIdAsync(int id);
        Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createCategoryDto);
        Task<bool> UpdateCategoryAsync(int id, UpdateCategoryDto updateCategoryDto);
        Task<bool> DeleteCategoryAsync(int id);
        Task<bool> UpdateCategoryStatusAsync(int id, UpdateCategoryStatusDto statusDto);
    }
}
