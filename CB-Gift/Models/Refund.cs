using CB_Gift.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CB_Gift.Models 
{
    [Table("Refunds")] 
    public partial class Refund
    {
        [Key]
        public int RefundId { get; set; }

        [Required]
        public int OrderId { get; set; }

        
        [Column(TypeName = "decimal(18, 2)")] // Kiểu dữ liệu chuẩn cho tiền tệ
        public decimal? Amount { get; set; }

        [Column(TypeName = "TEXT")]
        public string? Reason { get; set; }

        
        public string? RefundedByStaffId { get; set; } // ID của Staff thực hiện hoàn tiền

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? GatewayRefundId { get; set; } // Mã giao dịch hoàn tiền từ PayOS (nếu có)

        // --- Thuộc tính điều hướng (Navigation Properties) ---

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }

        [ForeignKey("RefundedByStaffId")]
        public virtual AppUser RefundedByStaff { get; set; }
    }
}