using CB_Gift.DTOs;
using System.Threading.Tasks;

namespace CB_Gift.Services.IService
{
    public interface IRefundService
    {
        /// <summary>
        /// (SELLER) Gửi yêu cầu hoàn tiền cho một đơn hàng.
        /// </summary>
        Task RequestRefundAsync(int orderId, SellerRefundRequestDto request, string sellerId);

        /// <summary>
        /// (STAFF/MANAGER) Xem xét (chấp nhận/từ chối) một yêu cầu hoàn tiền.
        /// </summary>
        Task ReviewRefundAsync(int refundId, StaffReviewRefundDto request, string staffId);
    }
}