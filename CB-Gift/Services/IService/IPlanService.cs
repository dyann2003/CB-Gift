using CB_Gift.Models;

namespace CB_Gift.Services.IService
{
    public interface IPlanService
    {
        /// Gom tất cả các OrderDetail đã submit (StatusOrder = 2) chưa có PlanDetail và tạo Plan + PlanDetail tương ứng.
        Task GroupSubmittedOrdersAsync(string createdUserId);
    }
}
