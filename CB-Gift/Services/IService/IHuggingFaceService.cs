namespace CB_Gift.Services.IService
{
    public interface IHuggingFaceService
    {
        // Chức năng: Text-to-Image
        Task<string> GenerateImageAsync(string prompt);

        // Chức năng: Image-to-Image
        Task<string> GenerateImageFromImageAsync(IFormFile imageFile, string prompt, float strength = 0.7f);
    }
}
