using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CB_Gift.Models
{
    public class Discount
    {
        [Key]
        public int DiscountId { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } // Mã code, ví dụ: "BLACKFRIDAY"

        [Required]
        [StringLength(255)]
        public string Description { get; set; } // Mô tả

        [Required]
        public string DiscountType { get; set; } // "Percentage" (Phần trăm) hoặc "FixedAmount" (Số tiền cố định)

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Value { get; set; } // 10 (cho 10%) hoặc 50000 (cho 50.000 VNĐ)

        [Column(TypeName = "decimal(18, 2)")]
        public decimal MinApplicableAmount { get; set; } = 0; // Số tiền tối thiểu để áp dụng

        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? EndDate { get; set; } // Null nếu không hết hạn

        public bool IsActive { get; set; } = true;
    }
}
