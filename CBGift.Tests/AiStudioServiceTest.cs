using CB_Gift.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
                return Task.FromResult(Responder(request));
            }
        }

        private static IConfiguration MakeConfig(string stabilityKey = "sk_test_123")
        {
            var dict = new Dictionary<string, string?>
            {
                ["StabilityAiSettings:ApiKey"] = stabilityKey
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(dict!)
                .Build();
        }

        private static (AiStudioService svc, CapturingHandler handler) MakeServiceUnderTest(
            Func<HttpRequestMessage, HttpResponseMessage> responder,
            string stabilityKey = "sk_test_123")
        {
            var handler = new CapturingHandler(responder);
            var httpClient = new HttpClient(handler);
            var config = MakeConfig(stabilityKey);
            ILogger<AiStudioService> logger = NullLogger<AiStudioService>.Instance;

            var svc = new AiStudioService(config, logger);

            var httpField = typeof(AiStudioService).GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance);
            if (httpField == null) throw new InvalidOperationException("Không tìm thấy field _httpClient trong AiStudioService.");

            httpField.SetValue(svc, httpClient);

            return (svc, handler);
        }

        private static string MakeDataUrlPngBase64(byte[] png)
            => "data:image/png;base64," + Convert.ToBase64String(png);

        private static string MakeValidInputBase64()
        {
            var tinyPng = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGP8z/CfAQAFNwJfQxGQ1wAAAABJRU5ErkJggg=="
            );
            return MakeDataUrlPngBase64(tinyPng);
        }

        private static MultipartFormDataContent GetMultipart(HttpRequestMessage req)
        {
            Assert.NotNull(req.Content);
            Assert.IsType<MultipartFormDataContent>(req.Content);
            return (MultipartFormDataContent)req.Content!;
        }

        private static HttpContent? FindPart(MultipartFormDataContent mp, string name)
        {
            foreach (var part in mp)
            {
                var n = part.Headers.ContentDisposition?.Name?.Trim('"');
                if (string.Equals(n, name, StringComparison.Ordinal)) return part;
            }
            return null;
        }

        private static string ReadPartString(HttpContent part)
            => part.ReadAsStringAsync().GetAwaiter().GetResult();

        [Fact]
        public async Task GenerateStructure_Success_Defaults_ReturnsDataUrlPng_And_SendsRequiredParts()
        {
            var fakeOut = new byte[] { 1, 2, 3, 4 };

            var (svc, _) = MakeServiceUnderTest(req =>
            {
                Assert.Equal("https://api.stability.ai/v2beta/stable-image/control/structure", req.RequestUri!.ToString());
                Assert.Equal(HttpMethod.Post, req.Method);

                Assert.Equal("Bearer", req.Headers.Authorization?.Scheme);
                Assert.False(string.IsNullOrWhiteSpace(req.Headers.Authorization?.Parameter));
                Assert.Contains(req.Headers.Accept, h => h.MediaType == "image/*");

                var mp = GetMultipart(req);

                var imagePart = FindPart(mp, "image");
                var promptPart = FindPart(mp, "prompt");
                var strengthPart = FindPart(mp, "control_strength");
                var stylePart = FindPart(mp, "style_preset");
                var seedPart = FindPart(mp, "seed");
                var fmtPart = FindPart(mp, "output_format");
                var negPart = FindPart(mp, "negative_prompt");

                Assert.NotNull(imagePart);
                Assert.Equal("image/png", imagePart!.Headers.ContentType?.MediaType);
                Assert.Equal("input.png", imagePart.Headers.ContentDisposition?.FileName?.Trim('"'));

                Assert.NotNull(promptPart);
                Assert.Equal("my prompt", ReadPartString(promptPart!));

                Assert.NotNull(strengthPart);
                ReadPartString(strengthPart!).ShouldBeInvariant("0.7");

                Assert.NotNull(stylePart);
                Assert.Equal("photographic", ReadPartString(stylePart!));

                Assert.Null(seedPart);

                Assert.NotNull(fmtPart);
                Assert.Equal("png", ReadPartString(fmtPart!));

                Assert.NotNull(negPart);
                Assert.False(string.IsNullOrWhiteSpace(ReadPartString(negPart!)));

                var resp = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(fakeOut)
                };
                resp.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                return resp;
            });

            var input = MakeValidInputBase64();
            var result = await svc.GenerateStructureImageAsync(input, "my prompt");

            Assert.StartsWith("data:image/png;base64,", result);
            var b64 = result.Substring("data:image/png;base64,".Length);
            Convert.FromBase64String(b64).ShouldEqual(fakeOut);
        }

        [Fact]
        public async Task GenerateStructure_Sends_Seed_When_GreaterThanZero()
        {
            var (svc, _) = MakeServiceUnderTest(req =>
            {
                var mp = GetMultipart(req);

                var seedPart = FindPart(mp, "seed");
                Assert.NotNull(seedPart);
                Assert.Equal("12345", ReadPartString(seedPart!));

                var resp = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(new byte[] { 9 })
                };
                resp.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                return resp;
            });

            var input = MakeValidInputBase64();
            var result = await svc.GenerateStructureImageAsync(
                input,
                "p",
                stylePreset: "photographic",
                controlStrength: 0.7,
                seed: 12345,
                outputFormat: "png");

            Assert.StartsWith("data:image/png;base64,", result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GenerateStructure_DoesNotSend_StylePreset_When_NullOrWhitespace(string? stylePreset)
        {
            var (svc, _) = MakeServiceUnderTest(req =>
            {
                var mp = GetMultipart(req);

                var stylePart = FindPart(mp, "style_preset");
                Assert.Null(stylePart);

                var resp = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(new byte[] { 7 })
                };
                resp.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                return resp;
            });

            var input = MakeValidInputBase64();
            var result = await svc.GenerateStructureImageAsync(
                input,
                "p",
                stylePreset: stylePreset,
                controlStrength: 0.7,
                seed: 0,
                outputFormat: "png");

            Assert.StartsWith("data:image/png;base64,", result);
        }

        [Theory]
        [InlineData("jpeg", "data:image/jpeg;base64,")]
        [InlineData("png", "data:image/png;base64,")]
        [InlineData("webp", "data:image/png;base64,")]
        public async Task GenerateStructure_OutputFormat_Validates_And_ReturnsExpectedMimePrefix(string outputFormat, string expectedPrefix)
        {
            var (svc, _) = MakeServiceUnderTest(req =>
            {
                var mp = GetMultipart(req);
                var fmtPart = FindPart(mp, "output_format");
                Assert.NotNull(fmtPart);

                var final = ReadPartString(fmtPart!);
                var valid = new[] { "png", "jpeg", "webp" };
                Assert.Contains(final, valid);

                var resp = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(new byte[] { 1, 1 })
                };
                resp.Content.Headers.ContentType = new MediaTypeHeaderValue(final == "jpeg" ? "image/jpeg" : "image/png");
                return resp;
            });

            var input = MakeValidInputBase64();
            var result = await svc.GenerateStructureImageAsync(
                input,
                "p",
                stylePreset: "photographic",
                controlStrength: 0.7,
                seed: 0,
                outputFormat: outputFormat);

            Assert.StartsWith(expectedPrefix, result);
        }

        [Fact]
        public async Task GenerateStructure_InvalidOutputFormat_DefaultsToPng()
        {
            var (svc, _) = MakeServiceUnderTest(req =>
            {
                var mp = GetMultipart(req);
                var fmtPart = FindPart(mp, "output_format");
                Assert.NotNull(fmtPart);
                Assert.Equal("png", ReadPartString(fmtPart!));

                var resp = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(new byte[] { 2 })
                };
                resp.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                return resp;
            });

            var input = MakeValidInputBase64();
            var result = await svc.GenerateStructureImageAsync(
                input,
                "p",
                stylePreset: "photographic",
                controlStrength: 0.7,
                seed: 0,
                outputFormat: "tiff");

            Assert.StartsWith("data:image/png;base64,", result);
        }

        [Fact]
        public async Task GenerateStructure_ControlStrength_IsInvariantCulture_OneDecimal()
        {
            var current = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("vi-VN");

                var (svc, _) = MakeServiceUnderTest(req =>
                {
                    var mp = GetMultipart(req);
                    var strengthPart = FindPart(mp, "control_strength");
                    Assert.NotNull(strengthPart);

                    var s = ReadPartString(strengthPart!);
                    Assert.Equal("0.7", s);

                    var resp = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(new byte[] { 3 })
                    };
                    resp.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                    return resp;
                });

                var input = MakeValidInputBase64();
                var result = await svc.GenerateStructureImageAsync(
                    input,
                    "p",
                    stylePreset: "photographic",
                    controlStrength: 0.7,
                    seed: 0,
                    outputFormat: "png");

                Assert.StartsWith("data:image/png;base64,", result);
            }
            finally
            {
                CultureInfo.CurrentCulture = current;
            }
        }

        [Fact]
        public async Task GenerateStructure_InvalidBase64_Throws_WithClearMessage()
        {
            var (svc, _) = MakeServiceUnderTest(_ => new HttpResponseMessage(HttpStatusCode.OK));

            var bad = "data:image/png;base64,@@@@";

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                svc.GenerateStructureImageAsync(bad, "p"));

            Assert.Contains("Invalid Base64 image string.", ex.Message);
        }

        [Fact]
        public async Task GenerateStructure_EmptyBase64_Throws_WithClearMessage()
        {
            var (svc, _) = MakeServiceUnderTest(_ => new HttpResponseMessage(HttpStatusCode.OK));

            var empty = "data:image/png;base64,";

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                svc.GenerateStructureImageAsync(empty, "p"));

            Assert.Contains("Invalid Base64 image string.", ex.Message);
        }

        [Fact]
        public async Task GenerateStructure_ApiError_Throws_WithBody()
        {
            var (svc, _) = MakeServiceUnderTest(_ =>
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("{\"error\":\"bad input\"}", Encoding.UTF8, "application/json")
                };
            });

            var input = MakeValidInputBase64();

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                svc.GenerateStructureImageAsync(input, "p"));

            Assert.Contains("Stability AI Error", ex.Message);
            Assert.Contains("bad input", ex.Message);
        }

        [Fact]
        public async Task GenerateStructure_SetsAuthHeader_FromConfigKey()
        {
            var (svc, _) = MakeServiceUnderTest(req =>
            {
                Assert.Equal("Bearer", req.Headers.Authorization?.Scheme);
                Assert.Equal("live_key_999", req.Headers.Authorization?.Parameter);

                var resp = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(new byte[] { 1, 2, 3 })
                };
                resp.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                return resp;
            }, stabilityKey: "live_key_999");

            var input = MakeValidInputBase64();
            var result = await svc.GenerateStructureImageAsync(input, "p");

            Assert.StartsWith("data:image/png;base64,", result);
        }
    }

    internal static class TestByteHelpers
    {
        public static void ShouldEqual(this byte[] actual, byte[] expected)
        {
            Assert.Equal(expected, actual);
        }

        public static void ShouldBeInvariant(this string actual, string expected)
        {
            Assert.Equal(expected, actual);
        }
    }
}
