using System.ComponentModel.DataAnnotations;

namespace CB_Gift.DTOs
{
    public class CancelDto
    {
    }
    /// <summary>
    /// DTO cho Seller gửi yêu cầu hủy (chỉ cần lý do).
    /// </summary>
    public class CancelRequestDto
    {
        [Required]
        public string Reason { get; set; }
    }

    /// <summary>
    /// DTO cho Staff/Manager xem xét yêu cầu hủy.
    /// </summary>
    public class ReviewCancelRequestDto
    {
        [Required]
        public bool Approved { get; set; } // true = Chấp nhận, false = Từ chối

        /// <summary>
        /// Lý do từ chối (bắt buộc nếu Approved = false).
        /// </summary>
        public string? RejectionReason { get; set; }
    }
}
