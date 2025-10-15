using CB_Gift.DTOs;

namespace CB_Gift.Services.IService
{
    public interface IDesignerTaskService
    {
        Task<IEnumerable<DesignTaskDto>> GetAssignedTasksAsync(string designerId);
        Task<bool> UploadDesignFileAsync(int orderDetailId, string designerId, UploadDesignDto dto);
        Task<bool> AssignDesignerToOrderDetailAsync(int orderDetailId, string designerUserId, string sellerId);
        Task<bool> AssignDesignerToOrderAsync(int orderId, string designerUserId, string sellerId);
        Task<bool> UpdateStatusAsync(int orderDetailId, int newStatus);
    }
}
