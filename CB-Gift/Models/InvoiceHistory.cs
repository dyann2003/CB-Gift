using CB_Gift.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CB_Gift.Models
{
    public class InvoiceHistory
    {
        [Key]
        public int HistoryId { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        [Required]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        public string? UserId { get; set; } // Null nếu hành động do hệ thống thực hiện

        [Required]
        [StringLength(255)]
        public string Action { get; set; }

        // --- Navigation Properties ---
        [ForeignKey("InvoiceId")]
        public virtual Invoice Invoice { get; set; }

        [ForeignKey("UserId")]
        public virtual AppUser? User { get; set; }
    }
}