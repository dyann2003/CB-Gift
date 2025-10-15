using CB_Gift.DTOs;

namespace CB_Gift.Services.IService
{
    public interface IDesignerSellerService
    {
        Task<bool> AssignDesignerToSellerAsync(AssignDesignerDto dto, string managerId);
        Task<bool> RemoveDesignerFromSellerAsync(AssignDesignerDto dto);
        Task<IEnumerable<DesignerSellerDto>> GetDesignersForSellerAsync(string sellerId);
        Task<IEnumerable<DesignerSellerDto>> GetSellersForDesignerAsync(string designerId);
    }
}
