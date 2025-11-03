using CB_Gift.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CB_Gift.Models
{
    [Table("OrderDetailLogs")] 
    public partial class OrderDetailLog
    {
        [Key] 
        public int OrderDetailLogId { get; set; }

        [Required]
        public int OrderDetailId { get; set; }

        [Required]
        public string ActorUserId { get; set; }

        [Required]
        [StringLength(50)]
        public string EventType { get; set; }

        [Column(TypeName = "TEXT")] 
        public string? Reason { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        [ForeignKey("OrderDetailId")] 
        public virtual OrderDetail OrderDetail { get; set; }

        [ForeignKey("ActorUserId")] 
        public virtual AppUser ActorUser { get; set; } 
    }
}