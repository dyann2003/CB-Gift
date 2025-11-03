using CB_Gift.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CB_Gift.Models
{
    [Table("CancellationRequests")] 
    public partial class CancellationRequest
    {
        [Key]
        public int CancellationRequestId { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public string RequestedByUserId { get; set; } // ID của Seller yêu cầu

        [Required]
        [Column(TypeName = "TEXT")] // Lý do yêu cầu
        public string RequestReason { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } // "Pending", "Approved", "Rejected"

        
        public string? PreviousStatusOrder { get; set; } // Trạng thái Order trước khi yêu cầu

        public string? ReviewedByStaffId { get; set; } // ID của Staff xử lý

        public DateTime? ReviewedAt { get; set; }

        [Column(TypeName = "TEXT")]
        public string? RejectionReason { get; set; } // Lý do Staff từ chối

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // --- Thuộc tính điều hướng (Navigation Properties) ---

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }

        [ForeignKey("RequestedByUserId")]
        public virtual AppUser RequestedByUser { get; set; }

        [ForeignKey("ReviewedByStaffId")]
        public virtual AppUser? ReviewedByStaff { get; set; }
    }
}