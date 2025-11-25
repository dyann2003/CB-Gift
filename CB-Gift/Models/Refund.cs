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

       
        public int? OrderId { get; set; }
        public int? OrderDetailId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; } // số tiền *yêu cầu* hoàn

        [Required]
        [Column(TypeName = "TEXT")]
        public string Reason { get; set; } 

        [StringLength(500)]
        public string? ProofUrl { get; set; } // Link bằng chứng của Seller

        [Required]
        [StringLength(20)]
        public string Status { get; set; } // "Pending", "Approved", "Rejected"

        [Required]
        public string RequestedBySellerId { get; set; }

        public string? ReviewedByStaffId { get; set; }

        public DateTime? ReviewedAt { get; set; } 

        [Column(TypeName = "TEXT")]
        public string? StaffRejectionReason { get; set; } // Lý do Staff từ chối

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; 

        [StringLength(100)]
        public string? GatewayRefundId { get; set; }

        // --- Thuộc tính điều hướng (Navigation Properties) ---

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [ForeignKey("OrderDetailId")] // Đảm bảo khớp với tên thuộc tính: OrderDetailId
        public virtual OrderDetail? OrderDetail { get; set; }

        [ForeignKey("RequestedBySellerId")]
        public virtual AppUser RequestedBySeller { get; set; } 

        [ForeignKey("ReviewedByStaffId")]
        public virtual AppUser? ReviewedByStaff { get; set; } 
    }
}