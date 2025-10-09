using CB_Gift.Data;
using CB_Gift.Models;
using CB_Gift.Services.IService;
using Microsoft.EntityFrameworkCore;

namespace CB_Gift.Services
{
    public class ImageManagementService : IImageManagementService
    {
        private readonly ICloudinaryService _cloudinaryService;
        private readonly CBGiftDbContext _context;

        public ImageManagementService(ICloudinaryService cloudinaryService, CBGiftDbContext context)
        {
            _cloudinaryService = cloudinaryService;
            _context = context;
        }

        public async Task<UploadedImage> UploadImageForUserAsync(Stream stream, string fileName, string userId)
        {
            // === BƯỚC 1: UPLOAD LÊN CLOUDINARY ===
            var uploadResult = await _cloudinaryService.UploadImageFromStreamAsync(stream, fileName, userId);

            // Chuẩn bị đối tượng để lưu vào DB
            var newImage = new UploadedImage
            {
                CloudinaryPublicId = uploadResult.PublicId,
                SecureUrl = uploadResult.SecureUrl.ToString(),
                OriginalFileName = fileName,
                UserId = userId,
                UploadedAt = DateTime.UtcNow
            };

            // === BƯỚC 2 & 3: LƯU VÀO DATABASE VỚI TRY-CATCH ===
            try
            {
                _context.UploadedImages.Add(newImage);
                await _context.SaveChangesAsync();
            }
            catch (Exception dbEx) // Bắt lỗi khi lưu vào database
            {
                // Nếu lưu thất bại, hãy cố gắng xóa file đã upload trên Cloudinary
                await _cloudinaryService.DeleteImageAsync(uploadResult.PublicId);

                // Ném lại lỗi gốc để controller có thể xử lý và báo cho người dùng
                throw new Exception("Lỗi khi lưu thông tin ảnh vào database.", dbEx);
            }

            // === BƯỚC 4: TRẢ VỀ KẾT QUẢ THÀNH CÔNG ===
            return newImage;
        }

        public async Task<IEnumerable<UploadedImage>> GetImagesByUserAsync(string userId)
        {
            return await _context.UploadedImages
                .Where(img => img.UserId == userId)
                .OrderByDescending(img => img.UploadedAt)
                .AsNoTracking() // Tối ưu hóa hiệu suất vì chỉ đọc dữ liệu
                .ToListAsync();
        }
    }
}
