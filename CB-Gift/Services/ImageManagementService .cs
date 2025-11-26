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
        public async Task<UploadedImage> UploadMediaForUserAsync(Stream stream, string fileName, string userId, string contentType)
        {
            string publicId = null;
            string secureUrl = null;
            bool isVideo = contentType.StartsWith("video/"); // Dùng biến này để rollback

            try
            {
                if (isVideo)
                {
                    var uploadResult = await _cloudinaryService.UploadVideoFromStreamAsync(stream, fileName, userId);
                    publicId = uploadResult.PublicId;
                    secureUrl = uploadResult.SecureUrl.ToString();
                }
                else if (contentType.StartsWith("image/"))
                {
                    var uploadResult = await _cloudinaryService.UploadImageFromStreamAsync(stream, fileName, userId);
                    publicId = uploadResult.PublicId;
                    secureUrl = uploadResult.SecureUrl.ToString();
                }
                else
                {
                    throw new Exception("Loại file không được hỗ trợ.");
                }

                // Tạo entity (Không có ResourceType)
                var newMedia = new UploadedImage
                {
                    CloudinaryPublicId = publicId,
                    SecureUrl = secureUrl,
                    OriginalFileName = fileName,
                    UserId = userId,
                    UploadedAt = DateTime.UtcNow
                };

                _context.UploadedImages.Add(newMedia);
                await _context.SaveChangesAsync();
                return newMedia;
            }
            catch (Exception dbEx)
            {
                // Rollback nếu lưu DB thất bại
                if (publicId != null)
                {
                    if (isVideo)
                    {
                        await _cloudinaryService.DeleteVideoAsync(publicId);
                    }
                    else
                    {
                        await _cloudinaryService.DeleteImageAsync(publicId);
                    }
                }
                throw new Exception("Lỗi khi lưu thông tin media vào database.", dbEx);
            }
        }
    }
}
