using CB_Gift.Models;

namespace CB_Gift.Services.IService
{
    public interface IImageManagementService
    {        
        Task<UploadedImage> UploadImageForUserAsync(Stream stream, string fileName, string userId);

        Task<IEnumerable<UploadedImage>> GetImagesByUserAsync(string userId);
    }
}
