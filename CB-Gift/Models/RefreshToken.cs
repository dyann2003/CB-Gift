using CB_Gift.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CB_Gift.Models
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime Expires { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime? Revoked { get; set; }

        // Token hết hạn nếu đã qua ngày Expires hoặc đã bị hủy (Revoked)
        public bool IsActive => Revoked == null && DateTime.UtcNow < Expires;

        [ForeignKey(nameof(UserId))]
        public AppUser? User { get; set; }
    }
}
