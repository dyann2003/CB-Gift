using CloudinaryDotNet.Actions;

namespace CB_Gift.Services.IService
{
    public interface ICloudinaryService
    {
        Task<ImageUploadResult> UploadImageAsync(IFormFile file);
        Task<ImageUploadResult> UploadImageFromStreamAsync(Stream stream, string fileName, string userId);
        Task DeleteImageAsync(string publicId);
        Task<VideoUploadResult> UploadVideoFromStreamAsync(Stream stream, string fileName, string userId);
        Task DeleteVideoAsync(string publicId);
    }
}
