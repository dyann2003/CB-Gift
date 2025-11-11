using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Hubs;
using CB_Gift.Models;
using CB_Gift.Services;
using CB_Gift.Services.IService;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CB_Gift.Tests.Services
{
    public class InvoiceServiceTests
    {
        private static CBGiftDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<CBGiftDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                // tránh cảnh báo transaction của InMemory
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .EnableSensitiveDataLogging()
                .Options;

            return new CBGiftDbContext(options);
        }

        private static IConfiguration CreateConfig()
        {
            var dict = new Dictionary<string, string?>
            {
                // các giá trị mock để khởi tạo PayOS (không gọi tới trong các test bên dưới)
                ["PayOS:ClientId"] = "dummy",
                ["PayOS:ApiKey"] = "dummy",
                ["PayOS:ChecksumKey"] = "dummy",
            };
            return new ConfigurationBuilder().AddInMemoryCollection(dict!).Build();
        }

        /// <summary>
        /// Tạo InvoiceService với đầy đủ mocks cho Notification & SignalR.
        /// </summary>
        private static InvoiceService CreateService(
            CBGiftDbContext db,
            out Mock<INotificationService> notifyMock,
            out Mock<IHubContext<NotificationHub>> hubCtxMock,
            out Mock<IHubClients> hubClientsMock,
            out Mock<IClientProxy> clientProxyMock,
            out ILogger<InvoiceService> logger)
        {
            // Notification
            notifyMock = new Mock<INotificationService>(MockBehavior.Strict);
            notifyMock
                .Setup(n => n.CreateAndSendNotificationAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // SignalR
            clientProxyMock = new Mock<IClientProxy>(MockBehavior.Strict);
            clientProxyMock
                .Setup(p => p.SendCoreAsync(
                    It.IsAny<string>(),
                    It.IsAny<object[]>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            hubClientsMock = new Mock<IHubClients>(MockBehavior.Strict);
            hubClientsMock
                .Setup(c => c.Group(It.IsAny<string>()))
                .Returns(clientProxyMock.Object);

            hubCtxMock = new Mock<IHubContext<NotificationHub>>(MockBehavior.Strict);
            hubCtxMock
                .SetupGet(h => h.Clients)
                .Returns(hubClientsMock.Object);

            logger = Mock.Of<ILogger<InvoiceService>>();

            return new InvoiceService(
                db,
                CreateConfig(),
                notifyMock.Object,
                hubCtxMock.Object,
                logger
            );
        }

        /// <summary>
        /// Bản rút gọn nếu không cần bắt mocks ở ngoài.
        /// </summary>
        private static InvoiceService CreateService(CBGiftDbContext db)
            => CreateService(db, out _, out _, out _, out _, out _);

        private static async Task SeedBasicAsync(CBGiftDbContext db)
        {
            var sellerId = "seller-1";
            var staffId = "staff-1";

            db.Users.Add(new AppUser { Id = sellerId, UserName = "seller@example.com" });
            db.Users.Add(new AppUser { Id = staffId, UserName = "staff@example.com" });

            db.Orders.AddRange(
                new Order
                {
                    OrderId = 101,
                    OrderCode = "ODR-101",
                    SellerUserId = sellerId,
                    OrderDate = new DateTime(2025, 10, 20),
                    PaymentStatus = "Unpaid",
                    StatusOrder = 1,
                    TotalCost = 100_000
                },
                new Order
                {
                    OrderId = 102,
                    OrderCode = "ODR-102",
                    SellerUserId = sellerId,
                    OrderDate = new DateTime(2025, 10, 21),
                    PaymentStatus = "Unpaid",
                    StatusOrder = 1,
                    TotalCost = 200_000
                },
                new Order
                {
                    OrderId = 103,
                    OrderCode = "ODR-103",
                    SellerUserId = sellerId,
                    OrderDate = new DateTime(2025, 10, 25),
                    PaymentStatus = "Paid", // sẽ bị loại ở kịch bản lọc Unpaid theo ngày
                    StatusOrder = 1,
                    TotalCost = 300_000
                }
            );

            await db.SaveChangesAsync();
        }
        // ---------- TESTS ----------

        [Fact]
        public async Task CreateInvoiceAsync_ByOrderIds_CreatesInvoice_Items_History()
        {
            // Arrange
            using var db = CreateDbContext(nameof(CreateInvoiceAsync_ByOrderIds_CreatesInvoice_Items_History));
            await SeedBasicAsync(db);
            var service = CreateService(db);

            var request = new CreateInvoiceRequest
            {
                SellerId = "seller-1",
                OrderIds = new List<int> { 101, 102 },
                Notes = "Test by OrderIds"
            };
            var staffId = "staff-1";

            // Act
            var invoice = await service.CreateInvoiceAsync(request, staffId);

            // Assert
            invoice.Should().NotBeNull();
            invoice.InvoiceId.Should().BeGreaterThan(0);
            invoice.SellerUserId.Should().Be("seller-1");
            invoice.Status.Should().Be("Issued");
            invoice.Subtotal.Should().Be(100_000 + 200_000);
            invoice.TotalAmount.Should().Be(invoice.Subtotal);
            invoice.InvoicePeriodStart.Should().Be(new DateTime(2025, 10, 20));
            invoice.InvoicePeriodEnd.Should().Be(new DateTime(2025, 10, 21));

            var items = await db.InvoiceItems.Where(ii => ii.InvoiceId == invoice.InvoiceId).ToListAsync();
            items.Should().HaveCount(2);
            items.Select(i => i.OrderId).Should().BeEquivalentTo(new[] { 101, 102 });

            var history = await db.InvoiceHistories.Where(h => h.InvoiceId == invoice.InvoiceId).ToListAsync();
            history.Should().ContainSingle(h => h.Action == "Invoice Created");
        }

        [Fact]
        public async Task CreateInvoiceAsync_ByDateRange_Filters_Unpaid_And_NotInvoiced()
        {
            // Arrange
            using var db = CreateDbContext(nameof(CreateInvoiceAsync_ByDateRange_Filters_Unpaid_And_NotInvoiced));
            await SeedBasicAsync(db);
            var service = CreateService(db);

            var request = new CreateInvoiceRequest
            {
                SellerId = "seller-1",
                StartDate = new DateTime(2025, 10, 20),
                EndDate = new DateTime(2025, 10, 25), // có order 103 Paid -> phải bị loại
                Notes = "Test by date range"
            };
            var staffId = "staff-1";

            // Act
            var invoice = await service.CreateInvoiceAsync(request, staffId);

            // Assert
            invoice.Should().NotBeNull();
            var items = await db.InvoiceItems.Where(ii => ii.InvoiceId == invoice.InvoiceId).ToListAsync();

            // Chỉ 101 & 102 (Unpaid) trong khoảng ngày
            items.Should().HaveCount(2);
            invoice.Subtotal.Should().Be(300_000);
            invoice.InvoicePeriodStart.Should().Be(new DateTime(2025, 10, 20));
            invoice.InvoicePeriodEnd.Should().Be(new DateTime(2025, 10, 25)); // theo yêu cầu: lấy từ request nếu có
            invoice.DueDate.Should().Be(new DateTime(2025, 10, 25).AddDays(15));
        }

        [Fact]
        public async Task CreateInvoiceAsync_InvalidOrderId_Throws_KeyNotFound()
        {
            using var db = CreateDbContext(nameof(CreateInvoiceAsync_InvalidOrderId_Throws_KeyNotFound));
            await SeedBasicAsync(db);
            var service = CreateService(db);

            var request = new CreateInvoiceRequest
            {
                SellerId = "seller-1",
                OrderIds = new List<int> { 101, 9999 } // 9999 không tồn tại
            };

            Func<Task> act = async () => await service.CreateInvoiceAsync(request, "staff-1");

            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*OrderID không tồn tại*");
        }

        [Fact]
        public async Task CreateInvoiceAsync_OrderNotBelongToSeller_Throws_InvalidOperation()
        {
            using var db = CreateDbContext(nameof(CreateInvoiceAsync_OrderNotBelongToSeller_Throws_InvalidOperation));
            await SeedBasicAsync(db);

            // Thêm một order của seller khác
            db.Users.Add(new AppUser { Id = "seller-2", UserName = "seller2@example.com" });
            db.Orders.Add(new Order
            {
                OrderId = 201,
                OrderCode = "ODR-201",
                SellerUserId = "seller-2",
                OrderDate = new DateTime(2025, 10, 22),
                PaymentStatus = "Unpaid",
                StatusOrder = 1,
                TotalCost = 150_000
            });
            await db.SaveChangesAsync();

            var service = CreateService(db);

            var request = new CreateInvoiceRequest
            {
                SellerId = "seller-1",
                OrderIds = new List<int> { 101, 201 } // 201 không thuộc seller-1
            };

            Func<Task> act = async () => await service.CreateInvoiceAsync(request, "staff-1");

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*không thuộc về Seller đã chọn*");
        }

        [Fact]
        public async Task CreateInvoiceAsync_NoOrdersAfterFilter_Throws_InvalidOperation()
        {
            using var db = CreateDbContext(nameof(CreateInvoiceAsync_NoOrdersAfterFilter_Throws_InvalidOperation));
            await SeedBasicAsync(db);
            var service = CreateService(db);

            var request = new CreateInvoiceRequest
            {
                SellerId = "seller-1",
                StartDate = new DateTime(2025, 10, 23), // sau 21
                EndDate = new DateTime(2025, 10, 24)    // trước 25 (103 là Paid)
            };

            Func<Task> act = async () => await service.CreateInvoiceAsync(request, "staff-1");

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không có đơn hàng mới hợp lệ*");
        }

        //[Fact]
        //public async Task GetInvoiceDetailsAsync_IncludeGraph_Works()
        //{
        //    using var db = CreateDbContext(nameof(GetInvoiceDetailsAsync_IncludeGraph_Works));
        //    await SeedBasicAsync(db);

        //    // Tạo 1 invoice + items để có dữ liệu
        //    var invoice = new Invoice
        //    {
        //        SellerUserId = "seller-1",
        //        CreatedByStaffId = "staff-1",
        //        InvoiceNumber = "INV-TEST",
        //        InvoicePeriodStart = new DateTime(2025, 10, 20),
        //        InvoicePeriodEnd = new DateTime(2025, 10, 21),
        //        DueDate = new DateTime(2025, 11, 05),
        //        Subtotal = 300_000,
        //        TotalAmount = 300_000,
        //        Status = "Issued",
        //        CreatedAt = DateTime.UtcNow
        //    };
        //    db.Invoices.Add(invoice);
        //    await db.SaveChangesAsync();

        //    db.InvoiceItems.AddRange(
        //        new InvoiceItem { InvoiceId = invoice.InvoiceId, OrderId = 101, Description = "Order ODR-101", Amount = 100_000 },
        //        new InvoiceItem { InvoiceId = invoice.InvoiceId, OrderId = 102, Description = "Order ODR-102", Amount = 200_000 }
        //    );

        //    // Thêm OrderDetails -> ProductVariant -> Product để test ThenInclude
        //    var product = new Product { ProductId = 1, ProductName = "Prod A" };
        //    var variant = new ProductVariant { ProductVariantId = 1, ProductId = 1 };
        //    db.Products.Add(product);
        //    db.ProductVariants.Add(variant);

        //    db.OrderDetails.AddRange(
        //        new OrderDetail { OrderDetailId = 1, OrderId = 101, ProductVariantId = 1, Quantity = 1, Price = 100_000 },
        //        new OrderDetail { OrderDetailId = 2, OrderId = 102, ProductVariantId = 1, Quantity = 2, Price = 100_000 }
        //    );

        //    await db.SaveChangesAsync();

        //    var service = CreateService(db);

        //    // Act
        //    var loaded = await service.GetInvoiceDetailsAsync(invoice.InvoiceId);

        //    // Assert
        //    loaded.Should().NotBeNull();
        //    loaded!.Items.Should().HaveCount(2);
        //    loaded.Items.Select(i => i.Order).Should().OnlyContain(o => o != null);
        //    // Kiểm tra ThenInclude sâu tới Product
        //    var anyDetailWithProduct =
        //        loaded.Items
        //              .SelectMany(i => i.Order!.OrderDetails)
        //              .Any(od => od.ProductVariant != null && od.ProductVariant.Product != null);
        //    anyDetailWithProduct.Should().BeTrue();
        //}

        //[Fact]
        //public async Task GetInvoicesForSellerAsync_Returns_OrderedDesc()
        //{
        //    using var db = CreateDbContext(nameof(GetInvoicesForSellerAsync_Returns_OrderedDesc));
        //    await SeedBasicAsync(db);

        //    db.Invoices.AddRange(
        //        new Invoice { SellerUserId = "seller-1", InvoiceNumber = "A", CreatedAt = new DateTime(2025, 10, 20), Status = "Issued", TotalAmount = 1 },
        //        new Invoice { SellerUserId = "seller-1", InvoiceNumber = "B", CreatedAt = new DateTime(2025, 10, 22), Status = "Issued", TotalAmount = 1 },
        //        new Invoice { SellerUserId = "seller-1", InvoiceNumber = "C", CreatedAt = new DateTime(2025, 10, 21), Status = "Issued", TotalAmount = 1 }
        //    );
        //    await db.SaveChangesAsync();

        //    var service = CreateService(db);

        //    // Act
        //    var list = (await service.GetInvoicesForSellerAsync("seller-1")).ToList();

        //    // Assert
        //    list.Should().HaveCount(3);
        //    list.Select(i => i.InvoiceNumber).Should().ContainInOrder("B", "C", "A");
        //}

        //[Fact]
        //public async Task GetAllInvoicesAsync_Includes_SellerUser_And_OrderedDesc()
        //{
        //    using var db = CreateDbContext(nameof(GetAllInvoicesAsync_Includes_SellerUser_And_OrderedDesc));
        //    await SeedBasicAsync(db);

        //    db.Invoices.AddRange(
        //        new Invoice { SellerUserId = "seller-1", InvoiceNumber = "A", CreatedAt = new DateTime(2025, 10, 20), Status = "Issued", TotalAmount = 1 },
        //        new Invoice { SellerUserId = "seller-1", InvoiceNumber = "B", CreatedAt = new DateTime(2025, 10, 22), Status = "Issued", TotalAmount = 1 }
        //    );
        //    await db.SaveChangesAsync();

        //    var service = CreateService(db);

        //    var list = (await service.GetAllInvoicesAsync()).ToList();

        //    list.Should().HaveCount(2);
        //    list[0].InvoiceNumber.Should().Be("B");
        //    list[0].SellerUser.Should().NotBeNull(); // Include(i => i.SellerUser)
        //}

        [Fact]
        public async Task LogWebhookAsync_SavesLog_And_ReturnsId()
        {
            using var db = CreateDbContext(nameof(LogWebhookAsync_SavesLog_And_ReturnsId));
            await SeedBasicAsync(db);
            var service = CreateService(db);

            var id = await service.LogWebhookAsync("PayOS", "{ \"test\": true }", "sig-abc");

            id.Should().BeGreaterThan(0);
            var log = await db.WebhookLogs.FindAsync(id);
            log.Should().NotBeNull();
            log!.PaymentGateway.Should().Be("PayOS");
            log.ProcessingStatus.Should().Be("Received");
        }

        // ===========================
        // TODO (gợi ý refactor để test PayOS):
        // 1) Tạo interface IPayOSClient (createPaymentLink, verifyPaymentWebhookData).
        // 2) Thêm constructor InvoiceService(CBGiftDbContext, IPayOSClient).
        // 3) Trong constructor cũ, khởi tạo PayOS thật rồi bọc trong adapter IPayOSClient.
        // 4) Trong unit test, mock IPayOSClient để kiểm tra:
        //    - CreatePaymentLinkAsync cập nhật PaymentLink từ checkoutUrl mock.
        //    - ProcessPayOSWebhookAsync đổi trạng thái invoice -> Paid, thêm Payment, cập nhật Orders.
        // ===========================
    }
}
