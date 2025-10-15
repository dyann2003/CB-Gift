using static CB_Gift.DTOs.StaffViewDtos;

namespace CB_Gift.Services.IService
{
    public interface IPlanService
    {
        /// Gom tất cả các OrderDetail đã submit (StatusOrder = 7) chưa có PlanDetail và tạo Plan + PlanDetail tương ứng dựa trên Category..
        Task GroupSubmittedOrdersAsync(string createdUserId);
        Task<IEnumerable<StaffCategoryPlanViewDto>> GetPlansForStaffViewAsync(int? categoryId, DateTime? selectedDate, string status);
    }
}
