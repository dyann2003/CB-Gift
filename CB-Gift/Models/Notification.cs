using CB_Gift.Data;

namespace CB_Gift.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string UserId { get; set; } // ID của người nhận thông báo
        public string Message { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string RedirectUrl { get; set; } // Link để điều hướng khi nhấn vào, vd: "/orders/123"
        public virtual AppUser User { get; set; }
    }
}
