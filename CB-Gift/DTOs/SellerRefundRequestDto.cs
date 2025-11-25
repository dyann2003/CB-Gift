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

    public class RefundItemRequestDto
    {
        // ID của chi tiết sản phẩm cần hoàn tiền
        [Required]
        public int OrderDetailId { get; set; }

        // Số tiền cụ thể yêu cầu hoàn lại cho item này
        [Required]
        public decimal RequestedAmount { get; set; }
    }
    public class RefundRequestDto
    {
        // Order ID gốc (Bắt buộc phải có để xác thực)
        [Required]
        public int OrderId { get; set; }

        [Required]
        public List<RefundItemRequestDto> Items { get; set; } = new List<RefundItemRequestDto>();

        [Required]
        [StringLength(1000)]
        public string Reason { get; set; }

        public string? ProofUrl { get; set; }
    }
    public class ReviewRefundDto
    {
        [Required]
        public bool Approved { get; set; }

        [StringLength(1000)]
        public string? RejectionReason { get; set; }
    }
    public class RefundDetailsDto
    {
        public int RefundId { get; set; }
        public int? OrderId { get; set; }
        public string OrderCode { get; set; }
        public string Status { get; set; }
        public decimal TotalRequestedAmount { get; set; }
        public string Reason { get; set; }
        public string? ProofUrl { get; set; }
        public string? StaffRejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<RefundItemDetailsDto> Items { get; set; } = new List<RefundItemDetailsDto>();
    }

    public class RefundItemDetailsDto
    {
        public int OrderDetailId { get; set; }
        public string ProductName { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal RefundAmount { get; set; }
        public string Sku { get; set; }
        public int Quantity { get; set; }
    }
    public class RefundRequestListDto
    {
        public int GroupId { get; set; }
        public int OrderId { get; set; }
        public string OrderCode { get; set; }
        public string Type { get; set; } = "REFUND"; // Cho biết loại
        public string TargetLevel { get; set; } // "ORDER-WIDE" hoặc "DETAIL"
        public string Status { get; set; }
        public decimal TotalRequestedAmount { get; set; } // Tổng số tiền yêu cầu trong nhóm
        public string ReasonSummary { get; set; } // Lý do tóm tắt
        public int CountOfItems { get; set; } // Số lượng OrderDetail bị ảnh hưởng
        public int PrimaryRefundId { get; set; } // ID của bản ghi đại diện (dùng để gọi Review Modal)
        public DateTime CreatedAt { get; set; }
    }

}