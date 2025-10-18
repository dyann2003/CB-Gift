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
            var uploadResult = await _cloudinaryService.UploadImageFromStreamAsync(stream, fileName, userId);

            var newImage = new UploadedImage
            {
                CloudinaryPublicId = uploadResult.PublicId,
                SecureUrl = uploadResult.SecureUrl.ToString(),
                OriginalFileName = fileName,
                UserId = userId,
                UploadedAt = DateTime.UtcNow
            };

            try
            {
                _context.UploadedImages.Add(newImage);
                await _context.SaveChangesAsync();
            }
            catch (Exception dbEx)
            {
                // Nếu lưu thất bại, xóa file đã upload trên Cloudinary
                await _cloudinaryService.DeleteImageAsync(uploadResult.PublicId);
                throw new Exception("Lỗi khi lưu thông tin ảnh vào database.", dbEx);
            }

            return newImage;
        }

        public async Task<IEnumerable<UploadedImage>> GetImagesByUserAsync(string userId)
        {
            return await _context.UploadedImages
                .Where(img => img.UserId == userId)
                .OrderByDescending(img => img.UploadedAt)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
