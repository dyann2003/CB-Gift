using System;
using System.Linq;
using System.Threading.Tasks;
using CB_Gift.Data;
using CB_Gift.Models;
using CB_Gift.Models.Enums;
using CB_Gift.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace CB_Gift.Tests.Services
{
    public class OrderDetailServiceTests
    {
        private static CBGiftDbContext NewDb()
        {
            var opt = new DbContextOptionsBuilder<CBGiftDbContext>()
                .UseInMemoryDatabase($"orderdetail-{Guid.NewGuid()}")
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new CBGiftDbContext(opt);
        }

        private static async Task<(CBGiftDbContext db, OrderDetailService svc, int od1, int od2, int orderId)> SeedAsync()
        {
            var db = NewDb();

            // Seed dữ liệu bắt buộc
            var endCustomer = new EndCustomer { CustId = 1, Name = "Customer A" };
            var product = new Product { ProductId = 1, ProductName = "Mug" };
            var variant = new ProductVariant { ProductVariantId = 10, ProductId = 1, Product = product, Sku = "MUG-RED" };
            var orderStatus = new OrderStatus
            {
                StatusId = 1,
                Code = "CREATED",
                NameVi = "Đơn mới tạo"
            };

            var order = new Order
            {
                OrderId = 100,
                OrderCode = "ORD100",
                OrderDate = DateTime.UtcNow,
                EndCustomerId = 1,
                EndCustomer = endCustomer,
                SellerUserId = "sellerA",
                StatusOrder = 1,
                StatusOrderNavigation = orderStatus,
                ProductionStatus = "CREATED",
                PaymentStatus = "UNPAID",
                ActiveTts = true,
                TotalCost = 100,
                CreationDate = DateTime.UtcNow,
                Tracking = "TRK001"
            };

            var odA = new OrderDetail
            {
                OrderDetailId = 1001,
                OrderId = 100,
                Order = order,
                ProductVariantId = 10,
                ProductVariant = variant,
                Quantity = 1,
                ProductionStatus = ProductionStatus.CREATED // 2
            };

            var odB = new OrderDetail
            {
                OrderDetailId = 1002,
                OrderId = 100,
                Order = order,
                ProductVariantId = 10,
                ProductVariant = variant,
                Quantity = 1,
                ProductionStatus = ProductionStatus.DESIGNING // 4
            };

            db.EndCustomers.Add(endCustomer);
            db.Products.Add(product);
            db.ProductVariants.Add(variant);
            db.OrderStatuses.Add(orderStatus);
            db.Orders.Add(order);
            db.OrderDetails.AddRange(odA, odB);
            await db.SaveChangesAsync();

            return (db, new OrderDetailService(db), odA.OrderDetailId, odB.OrderDetailId, order.OrderId);
        }

        [Fact]
        public async Task GetOrderDetailByIdAsync_Returns_Detail_With_Product_Navigation()
        {
            var (db, svc, od1, _, _) = await SeedAsync();

            var detail = await svc.GetOrderDetailByIdAsync(od1);

            detail.Should().NotBeNull();
            detail!.ProductVariant.Should().NotBeNull();
            detail.ProductVariant!.Product.Should().NotBeNull();
            detail.ProductVariant.Product!.ProductName.Should().Be("Mug");
        }

        [Fact]
        public async Task AcceptOrderDetailAsync_Sets_IN_PROD_And_Updates_OrderStatus_By_MinDetailStatus()
        {
            var (db, svc, od1, od2, orderId) = await SeedAsync();

            // Act: Accept (đặt ProductionStatus = IN_PROD = 9) cho od2
            var updated = await svc.AcceptOrderDetailAsync(od2);

            // Assert
            updated.Should().NotBeNull();
            updated!.ProductionStatus.Should().Be(ProductionStatus.IN_PROD);

            // Sau khi cập nhật: min(CREATED=2, IN_PROD=9) => 2 => map StatusOrder = 2
            var order = await db.Orders.Include(o => o.OrderDetails).FirstAsync(o => o.OrderId == orderId);
            order.StatusOrder.Should().Be(2);
        }

        [Fact]
        public async Task RejectOrderDetailAsync_Sets_QC_DONE_Logs_StatusUpdated_And_Updates_OrderStatus()
        {
            var (db, svc, od1, od2, orderId) = await SeedAsync();

            // Arrange: chuẩn bị DTO reject + actor
            var request = new CB_Gift.DTOs.QcRejectRequestDto
            {
                Reason = "Misaligned print area"
            };
            var qcUserId = "qc-user-01";

            // Act: Reject od1 -> đặt ProductionStatus = QC_DONE (11)
            var updated = await svc.RejectOrderDetailAsync(od1, request, qcUserId);

            // Assert 1: trạng thái dòng
            updated.Should().NotBeNull();
            updated!.ProductionStatus.Should().Be(ProductionStatus.QC_DONE);

            // Assert 2: log được ghi với STATUS_UPDATED (vì không phải PROD_REWORK)
            var logs = await db.OrderDetailLogs
                .Where(l => l.OrderDetailId == od1)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            logs.Should().NotBeEmpty();
            var log = logs.First();
            log.ActorUserId.Should().Be(qcUserId);
            log.Reason.Should().Be("Misaligned print area");
            log.EventType.Should().Be("STATUS_UPDATED");

            // Assert 3: cập nhật order theo min status sau reject
            // od1 = QC_DONE (11), od2 = DESIGNING (4) => min = 4 => map StatusOrder = 4
            var order = await db.Orders.Include(o => o.OrderDetails).FirstAsync(o => o.OrderId == orderId);
            order.StatusOrder.Should().Be(4);
        }

        [Fact]
        public async Task AcceptOrderDetailAsync_Returns_Null_When_NotFound()
        {
            var (db, svc, _, _, _) = await SeedAsync();

            var result = await svc.AcceptOrderDetailAsync(99999);
            result.Should().BeNull();
        }

        [Fact]
        public async Task RejectOrderDetailAsync_Returns_Null_When_NotFound()
        {
            var (db, svc, _, _, _) = await SeedAsync();

            var dto = new CB_Gift.DTOs.QcRejectRequestDto { Reason = "Invalid artwork" };
            var result = await svc.RejectOrderDetailAsync(99999, dto, "qc-123");
            result.Should().BeNull();
        }
    }
}
