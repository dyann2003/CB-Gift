using CB_Gift.DTOs;

namespace CB_Gift.Services.IService
{
    public interface IProductService
    {
        Task<IEnumerable<ProductDto>> GetAllAsync();
        Task<IEnumerable<ProductDto>> GetAllProductsHaveStatusTrueAsync(); // các sản phẩm active
        Task<IEnumerable<ProductDto>> GetHiddenProductsAsync();  // các sản phẩm bị ẩn
        Task<ProductDto?> GetByIdAsync(int id);
        Task<ProductDto> CreateAsync(ProductCreateDto dto);
        Task<ProductDto?> UpdateAsync(int id, ProductUpdateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> SoftDeleteProductAsync(int id);
        Task<bool> RestoreProductAsync(int id);
        //Update Status Product theo danh sách
        Task<(int updatedCount, IEnumerable<object> updatedProducts)> BulkUpdateStatusAsync(BulkUpdateProductStatusDto request, string updatedBy);
    }
}
