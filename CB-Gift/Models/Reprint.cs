using CB_Gift.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CB_Gift.Models
{
    public class Reprint
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OriginalOrderDetailId { get; set; }

        [ForeignKey(nameof(OriginalOrderDetailId))]
        public virtual OrderDetail? OriginalOrderDetail { get; set; }

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(450)]
        public string RequestedBy { get; set; }

        [ForeignKey(nameof(RequestedBy))]
        public virtual AppUser? RequestedByUser { get; set; }

        [StringLength(500)]
        public string? ProofUrl { get; set; } // Link bằng chứng của Seller

        [Required]
        [StringLength(20)]
        public string Status { get; set; } // "Pending", "Approved", "Rejected"

        [Column(TypeName = "TEXT")]
        public string? StaffRejectionReason { get; set; } // Lý do Manager từ chối

        [MaxLength(450)]
        public string? ManagerAcceptedBy { get; set; }

        public bool Processed { get; set; } = false;
    }
}
