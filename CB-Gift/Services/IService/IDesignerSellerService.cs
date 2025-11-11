using CB_Gift.DTOs;

namespace CB_Gift.Services.IService
{
    public interface IDesignerSellerService
    {
        Task<bool> AssignDesignerToSellerAsync(AssignDesignerDto dto, string managerId);
        Task<bool> RemoveDesignerFromSellerAsync(AssignDesignerDto dto);
        Task<IEnumerable<DesignerSellerDto>> GetDesignersForSellerAsync(string sellerId);
        Task<IEnumerable<DesignerSellerDto>> GetSellersForDesignerAsync(string designerId);
        Task<IEnumerable<SellerDesignerPairDto>> GetAllAssignmentsAsync();

        Task<(IEnumerable<SellerDesignerPairDto> Data, int Total)> GetAllAssignmentsPagedAsync(
          string? search, string? sellerName, int page, int pageSize);

 


    }
}
