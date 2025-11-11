using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Hubs;
using CB_Gift.Models;
using CB_Gift.Services;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CB_Gift.Tests.Services
{
    public class RefundServiceTests
    {
        // ===== Helpers =====
        private static DbContextOptions<CBGiftDbContext> NewInMemoryOptions()
            => new DbContextOptionsBuilder<CBGiftDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // DB riêng cho mỗi test
                .EnableSensitiveDataLogging()
                .Options;

        private static (Mock<IHubContext<NotificationHub>> hubCtx,
                        Mock<IHubClients> hubClients,
                        Mock<IClientProxy> clientProxy) CreateHubMock()
        {
            var mockClients = new Mock<IHubClients>();
            var mockProxy = new Mock<IClientProxy>();

            // Cho phép mọi SendCoreAsync để test không bị throw khi không setup cụ thể
            mockProxy
                .Setup(p => p.SendCoreAsync(
                    It.IsAny<string>(),
                    It.IsAny<object[]>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Clients.Group(...) -> IClientProxy
            mockClients
                .Setup(c => c.Group(It.IsAny<string>()))
                .Returns(mockProxy.Object);

            var mockHub = new Mock<IHubContext<NotificationHub>>();
            mockHub.SetupGet(h => h.Clients).Returns(mockClients.Object);

            return (mockHub, mockClients, mockProxy);
        }

        private static RefundService CreateService(CBGiftDbContext ctx,
                                                   Mock<INotificationService> notifMock,
                                                   Mock<IHubContext<NotificationHub>> hubMock)
        {
            ILogger<RefundService> logger = NullLogger<RefundService>.Instance;
            return new RefundService(ctx, notifMock.Object, hubMock.Object, logger);
        }

        private static Order MakeOrder(int id, string sellerId, string paymentStatus = "Paid") =>
            new Order
            {
                OrderId = id,
                SellerUserId = sellerId,
                PaymentStatus = paymentStatus,
                TotalCost = 123.45m,
                StatusOrder = 10,
                ProductionStatus = "IN_PROGRESS",
                OrderDetails = new List<OrderDetail>()
            };

        private static Refund MakeRefund(
           int refundId,
           int orderId,
           string sellerId,
           string status = "Pending",
           string reason = "seed-reason",
           string proofUrl = "http://example/proof.png")
           => new Refund
           {
               RefundId = refundId,
               OrderId = orderId,
               RequestedBySellerId = sellerId,
               Status = status,
               Amount = 50m,
               CreatedAt = DateTime.UtcNow,
               Reason = reason,            
               ProofUrl = proofUrl         
           };

        // ====== RequestRefundAsync ======

        [Fact]
        public async Task RequestRefund_CreatesRefund_And_SendsSignalR()
        {
            var options = NewInMemoryOptions();
            using var ctx = new CBGiftDbContext(options);

            // Seed order hợp lệ
            var order = MakeOrder(1001, "seller-1", "Paid");
            ctx.Orders.Add(order);
            await ctx.SaveChangesAsync();

            var (hubMock, _, proxy) = CreateHubMock();
            var notifMock = new Mock<INotificationService>(MockBehavior.Strict);

            var service = CreateService(ctx, notifMock, hubMock);

            var dto = new SellerRefundRequestDto
            {
                Reason = "Khách yêu cầu hoàn tiền",
                ProofUrl = "http://example/proof.png"
            };

            await service.RequestRefundAsync(1001, dto, "seller-1");

            // DB assert
            var refund = await ctx.Refunds.AsNoTracking().FirstOrDefaultAsync(r => r.OrderId == 1001);
            Assert.NotNull(refund);
            Assert.Equal("Pending", refund!.Status);
            Assert.Equal("seller-1", refund.RequestedBySellerId);
            Assert.Equal(order.TotalCost ?? 0, refund.Amount);

            // SignalR assert (verify SendCoreAsync)
            proxy.Verify(p => p.SendCoreAsync(
                    "NewRefundRequest",
                    It.Is<object[]>(args => args != null && args.Length == 1 && args[0] != null),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task RequestRefund_Throws_When_OrderNotFound_Or_NotSeller()
        {
            var options = NewInMemoryOptions();
            using var ctx = new CBGiftDbContext(options);

            ctx.Orders.Add(MakeOrder(2001, "seller-A", "Paid"));
            await ctx.SaveChangesAsync();

            var (hubMock, _, _) = CreateHubMock();
            var notifMock = new Mock<INotificationService>(MockBehavior.Strict);
            var service = CreateService(ctx, notifMock, hubMock);

            var dto = new SellerRefundRequestDto { Reason = "x", ProofUrl = "y" };

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.RequestRefundAsync(9999, dto, "seller-A"));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.RequestRefundAsync(2001, dto, "seller-B"));
        }

        [Fact]
        public async Task RequestRefund_Throws_When_Order_NotPaid()
        {
            var options = NewInMemoryOptions();
            using var ctx = new CBGiftDbContext(options);

            ctx.Orders.Add(MakeOrder(3001, "seller-3", "Pending"));
            await ctx.SaveChangesAsync();

            var (hubMock, _, _) = CreateHubMock();
            var notifMock = new Mock<INotificationService>(MockBehavior.Strict);
            var service = CreateService(ctx, notifMock, hubMock);

            var dto = new SellerRefundRequestDto { Reason = "x", ProofUrl = "y" };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.RequestRefundAsync(3001, dto, "seller-3"));
        }

        [Fact]
        public async Task RequestRefund_Throws_When_Existing_Pending_Refund()
        {
            var options = NewInMemoryOptions();
            using var ctx = new CBGiftDbContext(options);

            var order = MakeOrder(4001, "seller-4", "Paid");
            ctx.Orders.Add(order);
            ctx.Refunds.Add(MakeRefund(1, 4001, "seller-4", "Pending"));
            await ctx.SaveChangesAsync();

            var (hubMock, _, _) = CreateHubMock();
            var notifMock = new Mock<INotificationService>(MockBehavior.Strict);
            var service = CreateService(ctx, notifMock, hubMock);

            var dto = new SellerRefundRequestDto { Reason = "r", ProofUrl = "p" };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.RequestRefundAsync(4001, dto, "seller-4"));
        }

        // ====== ReviewRefundAsync ======

        [Fact]
        public async Task ReviewRefund_Throws_When_Refund_NotFound()
        {
            var options = NewInMemoryOptions();
            using var ctx = new CBGiftDbContext(options);

            var (hubMock, _, _) = CreateHubMock();
            var notifMock = new Mock<INotificationService>(MockBehavior.Strict);
            var service = CreateService(ctx, notifMock, hubMock);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.ReviewRefundAsync(999, new StaffReviewRefundDto { Approved = true }, "staff-1"));
        }

        [Fact]
        public async Task ReviewRefund_Throws_When_Refund_NotPending()
        {
            var options = NewInMemoryOptions();
            using var ctx = new CBGiftDbContext(options);

            var order = MakeOrder(5001, "seller-5", "Paid");
            ctx.Orders.Add(order);
            ctx.Refunds.Add(MakeRefund(51, 5001, "seller-5", "Approved")); // không Pending
            await ctx.SaveChangesAsync();

            var (hubMock, _, _) = CreateHubMock();
            var notifMock = new Mock<INotificationService>(MockBehavior.Strict);
            var service = CreateService(ctx, notifMock, hubMock);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ReviewRefundAsync(51, new StaffReviewRefundDto { Approved = true }, "staff-1"));
        }

        [Fact]
        public async Task ReviewRefund_Reject_Throws_When_MissingReason()
        {
            var options = NewInMemoryOptions();
            using var ctx = new CBGiftDbContext(options);

            var order = MakeOrder(5101, "seller-51", "Paid");
            ctx.Orders.Add(order);
            ctx.Refunds.Add(MakeRefund(511, 5101, "seller-51", "Pending"));
            await ctx.SaveChangesAsync();

            var (hubMock, _, _) = CreateHubMock();
            var notifMock = new Mock<INotificationService>(MockBehavior.Strict);
            var service = CreateService(ctx, notifMock, hubMock);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.ReviewRefundAsync(511, new StaffReviewRefundDto { Approved = false, RejectionReason = " " }, "staff-1"));
        }

        [Fact]
        public async Task ReviewRefund_Approve_Updates_Order_Removes_PlanDetails_And_Sends_Notifications()
        {
            var options = NewInMemoryOptions();
            using var ctx = new CBGiftDbContext(options);

            // Seed Order + Details + PlanDetails
            var order = MakeOrder(6001, "seller-6", "Paid");
            var detail = new OrderDetail { OrderDetailId = 1, OrderId = order.OrderId, PlanDetails = new List<PlanDetail>() };
            detail.PlanDetails.Add(new PlanDetail { PlanDetailId = 1, OrderDetailId = detail.OrderDetailId });
            detail.PlanDetails.Add(new PlanDetail { PlanDetailId = 2, OrderDetailId = detail.OrderDetailId });
            order.OrderDetails.Add(detail);

            ctx.Orders.Add(order);
            var refund = MakeRefund(61, order.OrderId, order.SellerUserId, "Pending");
            ctx.Refunds.Add(refund);
            await ctx.SaveChangesAsync();

            var (hubMock, _, proxy) = CreateHubMock();
            var notifMock = new Mock<INotificationService>();
            notifMock.Setup(n => n.CreateAndSendNotificationAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var service = CreateService(ctx, notifMock, hubMock);

            await service.ReviewRefundAsync(61, new StaffReviewRefundDto { Approved = true }, "staff-6");

            // Reload & assert
            var reloadedOrder = await ctx.Orders.Include(o => o.OrderDetails)
                                                .ThenInclude(od => od.PlanDetails)
                                                .FirstAsync(o => o.OrderId == 6001);

            Assert.Equal(18, reloadedOrder.StatusOrder);
            Assert.Equal("Refunded", reloadedOrder.PaymentStatus);
            Assert.Equal("CANCELED", reloadedOrder.ProductionStatus);
            Assert.All(reloadedOrder.OrderDetails, od => Assert.Empty(od.PlanDetails));

            var reloadedRefund = await ctx.Refunds.FirstAsync(r => r.RefundId == 61);
            Assert.Equal("Approved", reloadedRefund.Status);
            Assert.Equal("staff-6", reloadedRefund.ReviewedByStaffId);
            Assert.NotNull(reloadedRefund.ReviewedAt);

            // Notification sent to seller
            notifMock.Verify(n => n.CreateAndSendNotificationAsync(
                    order.SellerUserId,
                    It.Is<string>(msg => msg.Contains("CHẤP NHẬN")),
                    It.Is<string>(u => u.Contains($"/seller/orders/{order.OrderId}"))),
                Times.Once);

            // SignalR: OrderStatusChanged + RefundRequestReviewed (verify SendCoreAsync)
            proxy.Verify(p => p.SendCoreAsync(
                    "OrderStatusChanged",
                    It.Is<object[]>(args => args != null && args.Length == 1 && args[0] != null),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            proxy.Verify(p => p.SendCoreAsync(
                    "RefundRequestReviewed",
                    It.Is<object[]>(args => args != null && args.Length == 1 && args[0] != null),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ReviewRefund_Reject_Sets_Rejected_And_Sends_Notifications()
        {
            var options = NewInMemoryOptions();
            using var ctx = new CBGiftDbContext(options);

            var order = MakeOrder(7001, "seller-7", "Paid");
            ctx.Orders.Add(order);
            var refund = MakeRefund(71, order.OrderId, order.SellerUserId, "Pending");
            ctx.Refunds.Add(refund);
            await ctx.SaveChangesAsync();

            var (hubMock, _, proxy) = CreateHubMock();
            var notifMock = new Mock<INotificationService>();
            notifMock.Setup(n => n.CreateAndSendNotificationAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var service = CreateService(ctx, notifMock, hubMock);

            var rejectionReason = "Không đủ căn cứ";
            await service.ReviewRefundAsync(71, new StaffReviewRefundDto { Approved = false, RejectionReason = rejectionReason }, "staff-7");

            var reloadedOrder = await ctx.Orders.FirstAsync(o => o.OrderId == 7001);
            Assert.Equal(10, reloadedOrder.StatusOrder);            // giữ nguyên
            Assert.Equal("Paid", reloadedOrder.PaymentStatus);      // giữ nguyên

            var reloadedRefund = await ctx.Refunds.FirstAsync(r => r.RefundId == 71);
            Assert.Equal("Rejected", reloadedRefund.Status);
            Assert.Equal(rejectionReason, reloadedRefund.StaffRejectionReason);

            // Notification to seller (có chữ "TỪ CHỐI")
            notifMock.Verify(n => n.CreateAndSendNotificationAsync(
                    order.SellerUserId,
                    It.Is<string>(msg => msg.Contains("TỪ CHỐI")),
                    It.IsAny<string>()),
                Times.Once);

            // SignalR: OrderStatusChanged + RefundRequestReviewed (verify SendCoreAsync)
            proxy.Verify(p => p.SendCoreAsync(
                    "OrderStatusChanged",
                    It.Is<object[]>(args => args != null && args.Length == 1 && args[0] != null),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            proxy.Verify(p => p.SendCoreAsync(
                    "RefundRequestReviewed",
                    It.Is<object[]>(args => args != null && args.Length == 1 && args[0] != null),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
