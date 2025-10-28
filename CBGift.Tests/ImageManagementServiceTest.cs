using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;

using CB_Gift.Data;
using CB_Gift.Models;
using CB_Gift.Services;
using CB_Gift.Services.IService;

// Giả định service trả về ImageUploadResult của Cloudinary
// Nếu bạn dùng DTO khác, đổi "ImageUploadResult" thành DTO của bạn và set field tương ứng.
using CloudinaryDotNet.Actions;

namespace CB_Gift.Tests.Services
{
    public class ImageManagementServiceTests
    {
        private static CBGiftDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<CBGiftDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .EnableSensitiveDataLogging()
                .Options;

            return new CBGiftDbContext(options);
        }

        [Fact]
        public async Task UploadImageForUserAsync_Success_PersistsAndReturnsImage_NoDeleteCalled()
        {
            var dbName = Guid.NewGuid().ToString("N");
            await using var context = CreateDbContext(dbName);

            var mockCloudinary = new Mock<ICloudinaryService>();

            var publicId = "pid_123";
            var secureUrl = "https://res.cloudinary.com/demo/image/upload/v123/pid_123.jpg";

           
            mockCloudinary
                .Setup(s => s.UploadImageFromStreamAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new ImageUploadResult
                {
                    PublicId = publicId,
                    SecureUrl = new Uri(secureUrl)
                });

            mockCloudinary
                .Setup(s => s.DeleteImageAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var service = new ImageManagementService(mockCloudinary.Object, context);

            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var fileName = "photo.png";
            var userId = "user-001";

           
            var result = await service.UploadImageForUserAsync(stream, fileName, userId);

            Assert.NotNull(result);
            Assert.Equal(publicId, result.CloudinaryPublicId);
            Assert.Equal(secureUrl, result.SecureUrl);
            Assert.Equal(fileName, result.OriginalFileName);
            Assert.Equal(userId, result.UserId);
            Assert.True((DateTime.UtcNow - result.UploadedAt).TotalMinutes < 1); 

       
            var inDb = await context.UploadedImages.FirstOrDefaultAsync(x => x.CloudinaryPublicId == publicId);
            Assert.NotNull(inDb);

          
            mockCloudinary.Verify(s => s.DeleteImageAsync(It.IsAny<string>()), Times.Never);
        }

        /// <summary>
       
        private class ThrowingDbContext : CBGiftDbContext
        {
            public ThrowingDbContext(DbContextOptions<CBGiftDbContext> options) : base(options) { }

            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                throw new Exception("DB Save failed (simulated).");
            }
        }

        [Fact]
        public async Task UploadImageForUserAsync_WhenDbSaveFails_ThrowsAndDeletesUploadedFile()
        {

            var options = new DbContextOptionsBuilder<CBGiftDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString("N"))
                .Options;

            await using var throwingContext = new ThrowingDbContext(options);

            var mockCloudinary = new Mock<ICloudinaryService>();

            var publicId = "pid_fail_456";
            var secureUrl = "https://res.cloudinary.com/demo/image/upload/v123/pid_fail_456.jpg";

            mockCloudinary
                .Setup(s => s.UploadImageFromStreamAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new ImageUploadResult
                {
                    PublicId = publicId,
                    SecureUrl = new Uri(secureUrl)
                });

            mockCloudinary
                .Setup(s => s.DeleteImageAsync(publicId))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var service = new ImageManagementService(mockCloudinary.Object, throwingContext);

            using var stream = new MemoryStream(new byte[] { 9, 8, 7 });
            var fileName = "bad.png";
            var userId = "user-002";

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                service.UploadImageForUserAsync(stream, fileName, userId));

            Assert.Contains("Lỗi khi lưu thông tin ảnh vào database", ex.Message);

   
            mockCloudinary.Verify(s => s.DeleteImageAsync(publicId), Times.Once);
        }

        [Fact]
        public async Task GetImagesByUserAsync_FiltersByUserAndOrdersByUploadedAtDesc()
        {
            var dbName = Guid.NewGuid().ToString("N");
            await using var context = CreateDbContext(dbName);

            var mockCloudinary = new Mock<ICloudinaryService>();
            var service = new ImageManagementService(mockCloudinary.Object, context);

            var userA = "user-A";
            var userB = "user-B";

            var imgs = new List<UploadedImage>
            {
                new UploadedImage {
                    CloudinaryPublicId = "A1",
                    SecureUrl = "https://demo/A1.jpg",
                    OriginalFileName = "A1.png",
                    UserId = userA,
                    UploadedAt = DateTime.UtcNow.AddMinutes(-10)
                },
                new UploadedImage {
                    CloudinaryPublicId = "A2",
                    SecureUrl = "https://demo/A2.jpg",
                    OriginalFileName = "A2.png",
                    UserId = userA,
                    UploadedAt = DateTime.UtcNow.AddMinutes(-1)
                },
                new UploadedImage {
                    CloudinaryPublicId = "B1",
                    SecureUrl = "https://demo/B1.jpg",
                    OriginalFileName = "B1.png",
                    UserId = userB,
                    UploadedAt = DateTime.UtcNow.AddMinutes(-5)
                }
            };

            context.UploadedImages.AddRange(imgs);
            await context.SaveChangesAsync();

     
            var result = (await service.GetImagesByUserAsync(userA)).ToList();

        
            Assert.Equal(2, result.Count);
         
            Assert.Equal("A2", result[0].CloudinaryPublicId);
            Assert.Equal("A1", result[1].CloudinaryPublicId);
   
            Assert.DoesNotContain(result, x => x.UserId == userB);
        }
    }
}
