using CB_Gift.DTOs;

namespace CB_Gift.Services.IService
{
    public interface ICancellationService
    {
        /// <summary>
        /// (SELLER) Gửi yêu cầu hủy một đơn hàng.
        /// </summary>
        Task RequestCancellationAsync(int orderId, CancelRequestDto request, string sellerId);

        /// <summary>
        /// (STAFF/MANAGER) Xem xét (chấp nhận/từ chối) một yêu cầu hủy.
        /// </summary>
        Task ReviewCancellationAsync(int orderId, ReviewCancelRequestDto request, string staffId);
    }
}
