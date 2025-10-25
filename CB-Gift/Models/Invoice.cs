using CB_Gift.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CB_Gift.Models
{
    public class Invoice
    {
        [Key]
        public int InvoiceId { get; set; }

        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; }

        [Required]
        public string SellerUserId { get; set; }

        [Required]
        public string CreatedByStaffId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime InvoicePeriodStart { get; set; }

        [Required]
        public DateTime InvoicePeriodEnd { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TaxAmount { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal AmountPaid { get; set; } = 0;

        [Required]
        [StringLength(20)]
        public string Status { get; set; }

        public string? Notes { get; set; }

        [StringLength(500)]
        public string? PaymentLink { get; set; }

        // --- Navigation Properties ---
        [ForeignKey("SellerUserId")]
        public virtual AppUser SellerUser { get; set; }

        [ForeignKey("CreatedByStaffId")]
        public virtual AppUser CreatedByStaff { get; set; }

        public virtual ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ICollection<InvoiceHistory> History { get; set; } = new List<InvoiceHistory>();
    }
}
