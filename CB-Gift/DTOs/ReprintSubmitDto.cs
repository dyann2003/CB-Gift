using System.ComponentModel.DataAnnotations;

namespace CB_Gift.DTOs
{
    public class ReprintSubmitDto
    {
        [Required]
        public int OriginalOrderDetailId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; }

        [Required]
        [MaxLength(450)]
        public string RequestedByUserId { get; set; }

        public string? ProofUrl { get; set; }
    }
    public class ReprintItemRequestDto
    {
        [Required]
        public int OriginalOrderDetailId { get; set; } // Khóa ngoại tới OrderDetail gốc
    }
    public class SellerReprintRequestDto
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        public List<ReprintItemRequestDto> SelectedItems { get; set; } = new List<ReprintItemRequestDto>();

        [Required]
        [StringLength(1000)]
        public string Reason { get; set; } // Lý do chi tiết (Consolidated)

        [StringLength(500)]
        public string? ProofUrl { get; set; } // URL bằng chứng/file thiết kế (Consolidated)
    }
    public class ReprintRequestListDto
    {
        public int GroupId { get; set; } // ID nhóm/duy nhất cho frontend (tương đương Refund.Id)
        public int OrderId { get; set; }
        public string OrderCode { get; set; }
        public string Type { get; set; } = "REPRINT";
        public string TargetLevel { get; set; } = "DETAIL"; // Reprint thường là cấp độ OrderDetail
        public string Status { get; set; }
        public string ReasonSummary { get; set; } // Lý do tóm tắt từ Reprint.Reason
        public int CountOfItems { get; set; }
        public int OriginalOrderDetailId { get; set; } // ID OrderDetail bị ảnh hưởng
        public string ProductName { get; set; } // Tên sản phẩm/biến thể
        public int PrimaryReprintId { get; set; } // ID của bản ghi Reprint (dùng để gọi Review Modal)
        public DateTime CreatedAt { get; set; }
    }
}
