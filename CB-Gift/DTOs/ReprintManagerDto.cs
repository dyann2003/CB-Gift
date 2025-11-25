using System.ComponentModel.DataAnnotations;

namespace CB_Gift.DTOs
{
    public class ReprintManagerDto
    {
        [Required]
        public List<int> OriginalOrderDetailIds { get; set; }

        [Required]
        public string ManagerUserId { get; set; }

        // Lý do Manager từ chối 
        public string? RejectReason { get; set; }
    }
}
