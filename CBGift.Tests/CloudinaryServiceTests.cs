using CB_Gift.DTOs;
using CB_Gift.Services;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CBGift.Tests
{
    public class CloudinaryServiceTests
    {
        private static IOptions<CloudinarySettings> BuildOptions(
        string cloud = "demo",
        string key = "apiKey",
        string secret = "apiSecret",
        string folder = "CB-Gift-Uploads")
        {
            return Options.Create(new CloudinarySettings
            {
                CloudName = cloud,
                ApiKey = key,
                ApiSecret = secret,
                UploadFolder = folder
            });
        }

        private static (CloudinaryService sut, Mock<Cloudinary> mock, string folder)
            CreateServiceWithMock(IOptions<CloudinarySettings> opts = null)
        {
            opts ??= BuildOptions();
            var sut = new CloudinaryService(opts);

            var mockCloudinary = new Mock<Cloudinary>(new Account("demo", "xxx", "yyy")) { CallBase = true };

            var field = typeof(CloudinaryService)
                .GetField("_cloudinary", BindingFlags.NonPublic | BindingFlags.Instance);
            field!.SetValue(sut, mockCloudinary.Object);

            return (sut, mockCloudinary, opts.Value.UploadFolder ?? string.Empty);
        }

        [Fact]
        public void Constructor_Throws_When_ConfigMissing()
        {
            var badConfig = Options.Create(new CloudinarySettings
            {
                CloudName = "",
                ApiKey = "k",
                ApiSecret = "s"
            });

            Action act = () => new CloudinaryService(badConfig);
            act.Should().Throw<ArgumentException>().WithMessage("*Cloudinary*thiếu*");
        }

        [Fact]
        public async Task UploadImageAsync_ReturnsNull_When_FileIsNullOrEmpty()
        {
            var (sut, _, _) = CreateServiceWithMock();

            var result1 = await sut.UploadImageAsync(null);
            result1.Should().BeNull();

            var file = new Mock<IFormFile>();
            file.SetupGet(x => x.Length).Returns(0);
            var result2 = await sut.UploadImageAsync(file.Object);

            result2.Should().BeNull();
        }


        [Fact]
        public async Task UploadImageFromStreamAsync_Throws_When_StreamInvalid()
        {
            var (sut, _, _) = CreateServiceWithMock();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.UploadImageFromStreamAsync(null, "a.png", "user1"));

            var emptyStream = new MemoryStream();
            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.UploadImageFromStreamAsync(emptyStream, "a.png", "user1"));
        }

        [Fact]
        public async Task UploadImageFromStreamAsync_Throws_When_UserIdMissing()
        {
            var (sut, _, _) = CreateServiceWithMock();
            var ms = new MemoryStream(new byte[] { 1, 2, 3 });

            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.UploadImageFromStreamAsync(ms, "image.png", ""));
        }
        //[Fact]
        //public async Task UploadImageFromStreamAsync_Uploads_To_UserFolder()
        //{
        //    var (sut, mockCloud, folder) = CreateServiceWithMock();
        //    var ms = new MemoryStream(new byte[] { 1, 2, 3 });

        //    ImageUploadParams? captured = null;

        //    mockCloud
        //        .Setup(c => c.UploadAsync(
        //            It.IsAny<ImageUploadParams>(),
        //            It.IsAny<CancellationToken>()))
        //        .Callback<ImageUploadParams, CancellationToken>((p, _) => captured = p)
        //        .ReturnsAsync(new ImageUploadResult
        //        {
        //            PublicId = "returned-id",
        //            SecureUrl = new Uri("https://cloudinary.com/sample.jpg")
        //        });

        //    var res = await sut.UploadImageFromStreamAsync(ms, "photo.jpg", "userABC");

        //    res.PublicId.Should().Be("returned-id");
        //    captured.Should().NotBeNull();
        //    captured!.Folder.Should().Be($"{folder}/userABC");
        //    captured.Overwrite.Should().BeFalse();
        //    captured.File.Should().NotBeNull();
        //    captured.File.FileName.Should().Be("photo.jpg");
        //}

        //[Fact]
        //public async Task UploadImageFromStreamAsync_Throws_When_CloudinaryError()
        //{
        //    var (sut, mockCloud, _) = CreateServiceWithMock();
        //    var ms = new MemoryStream(new byte[] { 1, 2, 3 });

        //    mockCloud
        //        .Setup(c => c.UploadAsync(
        //            It.IsAny<ImageUploadParams>(),
        //            It.IsAny<CancellationToken>()))
        //        .ReturnsAsync(new ImageUploadResult
        //        {
        //            Error = new Error { Message = "Invalid signature" }
        //        });

        //    var ex = await Assert.ThrowsAsync<Exception>(() =>
        //        sut.UploadImageFromStreamAsync(ms, "test.jpg", "user123"));

        //    ex.Message.Should().Contain("Invalid signature");
        //}


        //[Fact]
        //public async Task DeleteImageAsync_Calls_CloudinaryDestroy()
        //{
        //    var (sut, mockCloud, _) = CreateServiceWithMock();

        //    string? destroyedId = null;

        //    mockCloud
        //        .Setup(c => c.DestroyAsync(It.IsAny<DeletionParams>()))
        //        .Callback<DeletionParams>(p => destroyedId = p.PublicId)
        //        .ReturnsAsync(new DeletionResult { Result = "ok" }); 

        //    await sut.DeleteImageAsync("publicId123");

        //    destroyedId.Should().Be("publicId123");
        //}

        //[Fact]
        //public async Task UploadImageAsync_Uploads_To_Configured_Folder()
        //{
        //    var (sut, mockCloud, folder) = CreateServiceWithMock();

        //    var fileBytes = new byte[] { 1, 2, 3 };
        //    var ms = new MemoryStream(fileBytes);

        //    var file = new Mock<IFormFile>();
        //    file.SetupGet(x => x.Length).Returns(fileBytes.Length);
        //    file.Setup(x => x.OpenReadStream()).Returns(ms);
        //    file.SetupGet(x => x.FileName).Returns("test.png");

        //    var fakeResult = new ImageUploadResult { PublicId = "test-public-id" };

        //    ImageUploadParams? captured = null;

        //    mockCloud
        //        .Setup(c => c.UploadAsync(
        //            It.IsAny<ImageUploadParams>(),
        //            It.IsAny<CancellationToken>()))
        //        .Callback<ImageUploadParams, CancellationToken>((p, _) => captured = p)
        //        .ReturnsAsync(fakeResult);

        //    var result = await sut.UploadImageAsync(file.Object);

        //    result.Should().NotBeNull();
        //    result!.PublicId.Should().Be("test-public-id");

        //    captured.Should().NotBeNull();
        //    captured!.Folder.Should().Be(folder);
        //    captured.File.Should().NotBeNull();
        //    captured.File.FileName.Should().Be("test.png");
        //}




    }
}
