using System.ComponentModel.DataAnnotations;

namespace CB_Gift.DTOs
{
    /// <summary>
    /// DTO cho Seller gửi yêu cầu hoàn tiền (sử dụng khi POST)
    /// </summary>
    public class SellerRefundRequestDto
    {
     /*   [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền hoàn phải lớn hơn 0.")]
        public decimal? Amount { get; set; }
*/
        [Required]
        public string Reason { get; set; }

        public string? ProofUrl { get; set; }
    }

    /// <summary>
    /// DTO cho Staff xem xét yêu cầu hoàn tiền (sử dụng khi POST)
    /// </summary>
    public class StaffReviewRefundDto
    {
        [Required]
        public bool Approved { get; set; } // true = Chấp nhận, false = Từ chối

        /// <summary>
        /// Lý do từ chối (bắt buộc nếu Approved = false).
        /// </summary>
        public string? RejectionReason { get; set; }
    }
}