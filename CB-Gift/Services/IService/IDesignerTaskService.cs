using CB_Gift.DTOs;
using CB_Gift.Models.Enums;

namespace CB_Gift.Services.IService
{
    public interface IDesignerTaskService
    {
        Task<IEnumerable<DesignTaskDto>> GetAssignedTasksAsync(string designerId);
        Task<bool> UploadDesignFileAsync(int orderDetailId, string designerId, UploadDesignDto dto);
        Task<bool> AssignDesignerToOrderDetailAsync(int orderDetailId, string designerUserId, string sellerId);
        Task<bool> AssignDesignerToOrderAsync(int orderId, string designerUserId, string sellerId);
        Task<bool> UpdateStatusAsync(int orderDetailId, ProductionStatus newStatus);
        Task<DesignTaskDetailDto> GetTaskDetailAsync(int orderDetailId, string designerId);
        Task<PaginatedResult<DesignTaskDto>> GetDesignedHistoryAsync(
         string designerId,
         int? productId, 
         string? sellerId,
         string? searchTerm,
         DateTime? startDate,
         DateTime? endDate,
         int page,
         int pageSize);

        //Lấy danh sách sản phẩm (duy nhất) mà designer đã làm ---
        Task<IEnumerable<ProductFilterDto>> GetDesignerProductsAsync(string designerId);
        Task<IEnumerable<SellerFilterDto>> GetDesignerSellersAsync(string designerId);
    }
}
