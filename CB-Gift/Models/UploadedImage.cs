using CB_Gift.Data;
using System.ComponentModel.DataAnnotations;

namespace CB_Gift.Models
{
    public class UploadedImage
    {

        [Key]
        public int Id { get; set; }

        [Required]
        public string CloudinaryPublicId { get; set; } // ID để quản lý trên Cloudinary

        [Required]
        public string SecureUrl { get; set; } // URL để hiển thị ảnh

        [Required]
        public string OriginalFileName { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // --- Mối quan hệ với người dùng ---
        [Required]
        public string UserId { get; set; } // Khóa ngoại tới bảng AppUser
        public AppUser User { get; set; }
    }
}
