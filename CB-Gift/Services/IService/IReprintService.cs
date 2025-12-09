// File: Controllers/IReprintService.cs

using CB_Gift.DTOs;

namespace CB_Gift.Services.IService
{
    public interface IReprintService
    {
        // User submit reprint request
        //Task SubmitReprintAsync(ReprintSubmitDto dto);

        // Manager approve reprint
        Task ApproveReprintAsync(ReprintManagerDto dto, string managerId);

        // Manager reject reprint
        Task RejectReprintAsync(ReprintManagerDto dto, string managerId);
        Task RequestReprintAsync(SellerReprintRequestDto request, string sellerId);
        Task<ReprintDetailsDto?> GetReprintDetailsAsync(int reprintId);
        Task<PaginatedResult<ReprintRequestListDto>> GetReviewReprintRequestsPaginatedAsync(
        string? staffId, // Dùng để xác thực (nếu cần)
        string? searchTerm,
        string? filterType,
        string? sellerIdFilter, // Reprint.RequestedBy tương đương
        string? statusFilter,
        int page,
        int pageSize);
    }
        
}
