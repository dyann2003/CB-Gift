using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CB_Gift.Models
{
    public class WebhookLog
    {
        [Key]
        public int WebhookLogId { get; set; }

        [Required]
        [StringLength(20)]
        public string PaymentGateway { get; set; } // Ví dụ: "PayOS", "SePay"

        [Required]
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string RawPayload { get; set; } // Lưu toàn bộ JSON gốc

        [StringLength(255)]
        public string? Signature { get; set; }

        [Required]
        [StringLength(20)]
        public string ProcessingStatus { get; set; } // Ví dụ: "Received", "Verified", "Processed", "Failed"

        public string? ErrorMessage { get; set; }

        public int? RelatedInvoiceId { get; set; }

        // --- Navigation Properties ---
        [ForeignKey("RelatedInvoiceId")]
        public virtual Invoice? Invoice { get; set; }
    }
}
