using System.ComponentModel.DataAnnotations;

namespace CB_Gift.DTOs
{
    public class ReprintSubmitDto
    {
        [Required]
        public int OriginalOrderDetailId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; }

        [Required]
        [MaxLength(450)]
        public string RequestedByUserId { get; set; }

        public string? ProofUrl { get; set; }
    }
}
