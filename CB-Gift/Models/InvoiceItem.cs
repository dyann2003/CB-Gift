using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CB_Gift.Models
{
    public class InvoiceItem
    {
        [Key]
        public int InvoiceItemId { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        [StringLength(255)]
        public string Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        // --- Navigation Properties ---
        [ForeignKey("InvoiceId")]
        public virtual Invoice Invoice { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }
    }
}