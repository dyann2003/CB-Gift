using CB_Gift.DTOs;
using CB_Gift.Services.IService;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace CB_Gift.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly string _uploadFolder;

        // Sử dụng Dependency Injection để nhận cấu hình từ appsettings.json
        public CloudinaryService(IOptions<CloudinarySettings> config)
        {
            var settings = config.Value;

            // Thêm kiểm tra để đảm bảo cấu hình đầy đủ
            if (string.IsNullOrEmpty(settings.CloudName) ||
                string.IsNullOrEmpty(settings.ApiKey) ||
                string.IsNullOrEmpty(settings.ApiSecret))
            {
                throw new ArgumentException("Cấu hình Cloudinary (CloudName, ApiKey, ApiSecret) bị thiếu.");
            }

            var account = new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret);
            _cloudinary = new Cloudinary(account);

            // Lấy folder từ cấu hình, nếu không có thì mặc định là chuỗi rỗng
            _uploadFolder = settings.UploadFolder ?? string.Empty;
        }

        public async Task<ImageUploadResult> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                // Trả về một kết quả không thành công (hoặc ném exception)
                return null;
            }

            // Tạo một luồng (stream) bộ nhớ để gửi tệp lên Cloudinary
            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(file.FileName, stream),
                Folder = _uploadFolder, // Lấy folder từ cấu hình
                                        // Thêm các tùy chọn xử lý ảnh tại đây nếu cần (ví dụ: Transformation)
            };

            // Thực hiện tải lên bất đồng bộ và trả về kết quả
            return await _cloudinary.UploadAsync(uploadParams);
        }

        public async Task<ImageUploadResult> UploadImageFromStreamAsync(Stream stream, string fileName, string userId)
        {
            // 1. Kiểm tra các tham số đầu vào
            if (stream == null || stream.Length == 0)
            {
                throw new ArgumentException("Dữ liệu stream không được rỗng.", nameof(stream));
            }
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("UserId không được rỗng.", nameof(userId));
            }

            // 2. Tạo đường dẫn thư mục theo UserId
            // Kết quả sẽ là: "CB-Gift-Uploads/your-user-id"
            var userFolderPath = $"{_uploadFolder}/{userId}";

            // 3. Tạo tên tệp duy nhất (PublicId) để tránh ghi đè file
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var uniqueSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            var publicId = $"{fileNameWithoutExtension}-{uniqueSuffix}";

            // 4. Chuẩn bị các tham số để gửi cho Cloudinary
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(fileName, stream),
                // Gán thư mục theo người dùng
                Folder = userFolderPath,
                // Gán tên file duy nhất
                PublicId = publicId,
                // Không cho phép ghi đè nếu có file trùng PublicId
                Overwrite = false
            };

            // 5. Gửi yêu cầu upload đến Cloudinary
            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            // 6. Kiểm tra lỗi trả về từ Cloudinary
            if (uploadResult.Error != null)
            {
                throw new Exception($"Lỗi từ Cloudinary: {uploadResult.Error.Message}");
            }

            // 7. Trả về kết quả nếu thành công
            return uploadResult;
        }

        public async Task DeleteImageAsync(string publicId)
        {
            var deletionParams = new DeletionParams(publicId);
            await _cloudinary.DestroyAsync(deletionParams);
        }
        // === PHƯƠNG THỨC MỚI CHO VIDEO ===
        public async Task<VideoUploadResult> UploadVideoFromStreamAsync(Stream stream, string fileName, string userId)
        {
            if (stream == null || stream.Length == 0)
                throw new ArgumentException("Dữ liệu stream không được rỗng.", nameof(stream));
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId không được rỗng.", nameof(userId));

            var userFolderPath = $"{_uploadFolder}/{userId}";
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var uniqueSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            var publicId = $"{fileNameWithoutExtension}-{uniqueSuffix}";

            var uploadParams = new VideoUploadParams()
            {
                File = new FileDescription(fileName, stream),
                Folder = userFolderPath,
                PublicId = publicId,
                Overwrite = false
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            if (uploadResult.Error != null)
                throw new Exception($"Lỗi từ Cloudinary: {uploadResult.Error.Message}");

            return uploadResult;
        }

        // === PHƯƠNG THỨC MỚI ĐỂ XÓA VIDEO ===
        public async Task DeleteVideoAsync(string publicId)
        {
            var deletionParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Video
            };
            await _cloudinary.DestroyAsync(deletionParams);
        }
    }
}

