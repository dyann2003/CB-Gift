using CB_Gift.DTOs;

namespace CB_Gift.Services.IService
{
    public interface IProductService
    {
        Task<IEnumerable<ProductDto>> GetAllAsync();
        Task<ProductDto?> GetByIdAsync(int id);
        Task<ProductDto> CreateAsync(ProductCreateDto dto);
        Task<ProductDto?> UpdateAsync(int id, ProductUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
