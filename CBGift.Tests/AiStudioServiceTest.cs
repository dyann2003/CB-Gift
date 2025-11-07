using CB_Gift.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CB_Gift.Tests.Services
{
    public class AiStudioServiceTests
    {
        // --- HttpMessageHandler để bắt & kiểm tra request ---
        private sealed class CapturingHandler : HttpMessageHandler
        {
            public HttpRequestMessage? LastRequest { get; private set; }
            public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; set; }

            public CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            {
                Responder = responder;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                var resp = Responder(request);
                return Task.FromResult(resp);
            }
        }

        private static IConfiguration MakeConfig(string stabilityKey = "sk_test_123", string geminiKey = "gm_test_456")
        {
            var dict = new Dictionary<string, string?>
            {
                ["StabilityAiSettings:ApiKey"] = stabilityKey,
                ["GeminiSettings:ApiKey"] = geminiKey
            };
            return new ConfigurationBuilder()
                .AddInMemoryCollection(dict!)
                .Build();
        }

        /// <summary>
        /// Tạo service dưới test. Nếu AiStudioService có ctor (IConfiguration, ILogger, HttpClient)
        /// thì dùng luôn. Nếu không, tạo bản mặc định rồi inject HttpClient qua reflection (fallback).
        /// </summary>
        private static (AiStudioService svc, CapturingHandler handler) MakeServiceUnderTest(
            Func<HttpRequestMessage, HttpResponseMessage> responder,
            string stabilityKey = "sk_test_123")
        {
            var handler = new CapturingHandler(responder);
            var httpClient = new HttpClient(handler);
            var config = MakeConfig(stabilityKey);
            ILogger<AiStudioService> logger = NullLogger<AiStudioService>.Instance;

            AiStudioService svc;

            // Ưu tiên ctor có HttpClient (nếu bạn đã thêm overload)
            var ctorWithHttp =
                typeof(AiStudioService).GetConstructor(new[] {
                    typeof(IConfiguration), typeof(ILogger<AiStudioService>), typeof(HttpClient)
                });

            if (ctorWithHttp != null)
            {
                svc = (AiStudioService)ctorWithHttp.Invoke(new object[] { config, logger, httpClient });
            }
            else
            {
                // Fallback: dùng ctor mặc định rồi "nhét" _httpClient qua reflection
                svc = new AiStudioService(config, logger);

                var httpField = typeof(AiStudioService).GetField("_httpClient",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (httpField == null)
                    throw new InvalidOperationException("Không tìm thấy field _httpClient trong AiStudioService.");

                // Bỏ readonly bằng cách thao tác trên handle runtime (chấp nhận cho mục đích unit test)
                httpField.SetValue(svc, httpClient);
            }

            return (svc, handler);
        }

        private static string MakeDataUrlPngBase64(byte[] png)
            => "data:image/png;base64," + Convert.ToBase64String(png);

        private static string MakeValidInputBase64()
        {
            // 1×1 PNG trong base64 (hợp lệ, nhỏ gọn)
            var tinyPng = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGP8z/CfAQAFNwJfQxGQ1wAAAABJRU5ErkJggg=="
            );
            return MakeDataUrlPngBase64(tinyPng);
        }

        [Fact]
        public async Task GenerateLineArt_Success_Returns_DataUrlPng_And_Uses_StaticInstructions_When_UserPrompt_Empty()
        {
            // Arrange: giả lập Stability trả ảnh PNG
            var fakeOut = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAYAAABytg0kAAAADUlEQVR4nGP8z/D/PwAHxAKm3f7KswAAAABJRU5ErkJggg=="
            );

            var (svc, _) = MakeServiceUnderTest(req =>
            {
                // Đảm bảo URL đúng
                Assert.Equal("https://api.stability.ai/v2beta/stable-image/control/sketch", req.RequestUri!.ToString());
                Assert.Equal(HttpMethod.Post, req.Method);

                // Header: Authorization, Accept
                Assert.True(req.Headers.Authorization?.Scheme == "Bearer");
                Assert.False(string.IsNullOrEmpty(req.Headers.Authorization?.Parameter));
                Assert.Contains(req.Headers.Accept, h => h.MediaType == "image/*");
                Assert.Contains(req.Headers.Accept, h => h.MediaType == "application/json");

                // Nội dung multipart
                Assert.IsType<MultipartFormDataContent>(req.Content);
                var mp = (MultipartFormDataContent)req.Content;

                // Thu gom các part theo tên
                HttpContent? imagePart = null;
                HttpContent? controlTypePart = null;
                HttpContent? promptPart = null;
                HttpContent? outFmtPart = null;

                foreach (var part in mp)
                {
                    var name = part.Headers.ContentDisposition?.Name?.Trim('"');
                    switch (name)
                    {
                        case "image": imagePart = part; break;
                        case "control_type": controlTypePart = part; break;
                        case "prompt": promptPart = part; break;
                        case "output_format": outFmtPart = part; break;
                    }
                }

                Assert.NotNull(imagePart);
                Assert.Equal("image/png", imagePart!.Headers.ContentType?.MediaType);
                Assert.Equal("image.png", imagePart!.Headers.ContentDisposition?.FileName?.Trim('"'));

                Assert.NotNull(controlTypePart);
                var control = controlTypePart!.ReadAsStringAsync().Result;
                Assert.Equal("sketch", control);

                Assert.NotNull(outFmtPart);
                Assert.Equal("png", outFmtPart!.ReadAsStringAsync().Result);

                Assert.NotNull(promptPart);

                // Kiểm tra rơi về staticInstructions khi userPrompt = "" (trống/whitespace)
                var staticInstr = typeof(AiStudioService)
                    .GetField("staticInstructions", BindingFlags.NonPublic | BindingFlags.Static)!
                    .GetRawConstantValue() as string;

                var prompt = promptPart!.ReadAsStringAsync().Result;
                Assert.False(string.IsNullOrWhiteSpace(prompt));
                Assert.Equal(staticInstr, prompt);

                // Trả về ảnh PNG
                var resp = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(fakeOut)
                };
                resp.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                return resp;
            });

            var input = MakeValidInputBase64();

            // Act (userPrompt rỗng -> dùng staticInstructions)
            var result = await svc.GenerateLineArtFromChibiAsync(input, "");

            // Assert: phải là data URL + base64
            Assert.StartsWith("data:image/png;base64,", result);
            var b64 = result.Substring("data:image/png;base64,".Length);
            var bytes = Convert.FromBase64String(b64);
            Assert.Equal(fakeOut, bytes);
        }

        [Fact]
        public async Task GenerateLineArt_Uses_UserPrompt_When_Provided()
        {
            var userPrompt = "Draw only clean black single-weight lines, no fills.";
            var (svc, _) = MakeServiceUnderTest(req =>
            {
                Assert.Equal("https://api.stability.ai/v2beta/stable-image/control/sketch", req.RequestUri!.ToString());
                Assert.IsType<MultipartFormDataContent>(req.Content);
                var mp = (MultipartFormDataContent)req.Content;

                HttpContent? promptPart = null;
                foreach (var part in mp)
                {
                    var name = part.Headers.ContentDisposition?.Name?.Trim('"');
                    if (name == "prompt") { promptPart = part; break; }
                }
                Assert.NotNull(promptPart);
                var prompt = promptPart!.ReadAsStringAsync().Result;
                Assert.Equal(userPrompt, prompt);

                var ok = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(new byte[] { 1, 2, 3 })
                };
                ok.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                return ok;
            });

            var input = MakeValidInputBase64();

            var result = await svc.GenerateLineArtFromChibiAsync(input, userPrompt);
            Assert.StartsWith("data:image/png;base64,", result);
        }

        [Fact]
        public async Task GenerateLineArt_InvalidBase64_Throws_WithClearMessage()
        {
            var (svc, _) = MakeServiceUnderTest(_ =>
            {
                // Sẽ không tới đây vì fail trước khi gọi HTTP
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            // Sai base64 (ký tự lạ)
            var bad = "data:image/png;base64,@@@@";

            var ex = await Assert.ThrowsAsync<Exception>(() => svc.GenerateLineArtFromChibiAsync(bad, ""));
            Assert.Contains("Invalid Base64 image string.", ex.Message);
        }

        [Fact]
        public async Task GenerateLineArt_EmptyBase64_Throws_WithClearMessage()
        {
            var (svc, _) = MakeServiceUnderTest(_ => new HttpResponseMessage(HttpStatusCode.OK));

            // Chuỗi empty sau khi tách prefix
            var empty = "data:image/png;base64,";

            var ex = await Assert.ThrowsAsync<Exception>(() => svc.GenerateLineArtFromChibiAsync(empty, ""));
            Assert.Contains("Invalid Base64 image string.", ex.Message);
        }

        [Fact]
        public async Task GenerateLineArt_ApiReturnsError_Throws_WithBody()
        {
            var (svc, _) = MakeServiceUnderTest(_ =>
            {
                var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("{\"error\":\"bad input\"}", Encoding.UTF8, "application/json")
                };
                return resp;
            });

            var input = MakeValidInputBase64();

            var ex = await Assert.ThrowsAsync<Exception>(() => svc.GenerateLineArtFromChibiAsync(input, ""));
            Assert.Contains("Stability AI error", ex.Message);
            Assert.Contains("bad input", ex.Message);
        }

        [Fact]
        public async Task GenerateLineArt_SetsAuthHeader_FromConfigKey()
        {
            var (svc, _) = MakeServiceUnderTest(req =>
            {
                Assert.Equal("Bearer", req.Headers.Authorization?.Scheme);
                Assert.Equal("live_key_999", req.Headers.Authorization?.Parameter);

                var ok = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(new byte[] { 1, 2, 3 })
                };
                ok.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                return ok;
            }, stabilityKey: "live_key_999");

            var input = MakeValidInputBase64();
            var result = await svc.GenerateLineArtFromChibiAsync(input, "");
            Assert.StartsWith("data:image/png;base64,", result);
        }
    }
}
