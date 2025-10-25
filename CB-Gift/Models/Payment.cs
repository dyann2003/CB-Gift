using CB_Gift.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CB_Gift.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } // Ví dụ: "PayOS", "Bank Transfer"

        [StringLength(100)]
        public string? TransactionId { get; set; } // Mã giao dịch từ cổng thanh toán

        [StringLength(255)]
        public string? Note { get; set; }

        public string? ProcessedByStaffId { get; set; }

        // --- Navigation Properties ---
        [ForeignKey("InvoiceId")]
        public virtual Invoice Invoice { get; set; }

        [ForeignKey("ProcessedByStaffId")]
        public virtual AppUser? ProcessedByStaff { get; set; }
    }
}