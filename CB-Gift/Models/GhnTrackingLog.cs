using System.ComponentModel.DataAnnotations;

namespace CB_Gift.Models
{
    public class GhnTrackingLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string OrderCode { get; set; } // Mã vận đơn (VD: L4ELQF)

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } // ready_to_pick, picking, delivered...

        public DateTime UpdatedDate { get; set; } = DateTime.Now;
    }
}
