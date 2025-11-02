using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Hubs;
using CB_Gift.Models;
using CB_Gift.Models.Enums;
using CB_Gift.Services;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;              // <<-- cần cho CancellationToken
using System.Threading.Tasks;
using Xunit;

namespace CB_Gift.Tests.Services
{
    public class DesignerTaskServiceTests
    {
        private static CBGiftDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<CBGiftDbContext>()
                .UseInMemoryDatabase(dbName)
                .EnableSensitiveDataLogging()
                .Options;
            return new CBGiftDbContext(options);
        }

        // Overload tiện dụng: cho phép gọi CreateService(db) không cần out
        private static DesignerTaskService CreateService(CBGiftDbContext ctx)
            => CreateService(ctx, out _, out _, out _, out _, out _, out _);

        private static DesignerTaskService CreateService(
            CBGiftDbContext ctx,
            out Mock<IImageManagementService> imageSvcMock,
            out Mock<INotificationService> notifySvcMock,
            out Mock<IHubContext<NotificationHub>> hubCtxMock,
            out Mock<IHubClients> hubClientsMock,
            out Mock<IClientProxy> clientProxyMock,
            out Mock<ILogger<DesignerTaskService>> loggerMock)
        {
            imageSvcMock = new Mock<IImageManagementService>(MockBehavior.Strict);

            notifySvcMock = new Mock<INotificationService>(MockBehavior.Strict);
            // Cho phép gọi ở các nhánh gửi thông báo
            notifySvcMock
                .Setup(s => s.CreateAndSendNotificationAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Mock SignalR hub
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

            loggerMock = new Mock<ILogger<DesignerTaskService>>();

            return new DesignerTaskService(
                ctx,
                imageSvcMock.Object,
                notifySvcMock.Object,
                hubCtxMock.Object,
                loggerMock.Object
            );
        }

        #region Helpers - Seed data
        private static Product MakeProduct(int id, string name)
        {
            return new Product
            {
                ProductId = id,
                ProductName = name,
                Describe = "Desc",
                Template = "Template"
            };
        }

        private static ProductVariant MakeVariant(int id, Product p)
        {
            return new ProductVariant
            {
                ProductVariantId = id,
                ProductId = p.ProductId,
                Product = p,
                LengthCm = 10m,
                HeightCm = 20m,
                WidthCm = 5m,
                ThicknessMm = "2",
                SizeInch = "8x10",
                Layer = "1",
                CustomShape = "false",
                Sku = "SKU-TEST"
            };
        }

        private static Order MakeOrder(int id, string code, string sellerId, int statusOrder)
        {
            return new Order
            {
                OrderId = id,
                OrderCode = code,
                SellerUserId = sellerId,
                StatusOrder = statusOrder,
                OrderDetails = new List<OrderDetail>()
            };
        }

        private static OrderDetail MakeOrderDetail(
            int id,
            Order order,
            ProductVariant variant,
            string? designerId,
            bool needDesign,
            ProductionStatus? prodStatus,
            string linkImg = "img",
            string linkThanks = "thanks",
            string note = "note")
        {
            var od = new OrderDetail
            {
                OrderDetailId = id,
                OrderId = order.OrderId,
                Order = order,
                ProductVariantId = variant.ProductVariantId,
                ProductVariant = variant,
                AssignedDesignerUserId = designerId,
                NeedDesign = needDesign,
                ProductionStatus = prodStatus,
                Quantity = 2,
                LinkImg = linkImg,
                LinkThanksCard = linkThanks,
                LinkFileDesign = null,
                Note = note,
                AssignedAt = DateTime.UtcNow
            };
            order.OrderDetails.Add(od);
            return od;
        }
        #endregion

        [Fact]
        public async Task GetAssignedTasksAsync_Filters_By_Designer_DesignFlags_Statuses_And_OrderStatus()
        {
            // Arrange
            var db = CreateDbContext(Guid.NewGuid().ToString());

            var service = CreateService(
                db,
                out var _img,
                out var _notify,
                out var _hub,
                out var _hubClients,
                out var _clientProxy,
                out var _logger);

            string designerId = "designer-1";
            string otherDesignerId = "designer-2";

            var p = MakeProduct(1, "Prod A");
            var v = MakeVariant(1, p);
            var v2 = MakeVariant(2, p);

            // Trạng thái order hợp lệ để lọc: {3,4,5,6}
            var orderOk = MakeOrder(1, "ORD-OK", "seller-1", statusOrder: 3);
            var orderBad = MakeOrder(2, "ORD-BAD", "seller-1", statusOrder: 2); // bị loại

            // Chi tiết đủ điều kiện (designer match, NeedDesign true, ProductionStatus hợp lệ)
            var od1 = MakeOrderDetail(1, orderOk, v, designerId, true, ProductionStatus.DESIGNING);
            // Bị loại vì Order.StatusOrder = 2
            var od2 = MakeOrderDetail(2, orderBad, v, designerId, true, ProductionStatus.DESIGNING);
            // Bị loại vì NeedDesign = false
            var od3 = MakeOrderDetail(3, orderOk, v, designerId, false, ProductionStatus.DESIGNING);
            // Bị loại vì designer khác
            var od4 = MakeOrderDetail(4, orderOk, v2, otherDesignerId, true, ProductionStatus.DESIGNING);
            // Bị loại vì ProductionStatus null
            var od5 = MakeOrderDetail(5, orderOk, v2, designerId, true, null);

            await db.Products.AddAsync(p);
            await db.ProductVariants.AddRangeAsync(v, v2);
            await db.Orders.AddRangeAsync(orderOk, orderBad);
            await db.SaveChangesAsync();

            // Act
            var tasks = (await service.GetAssignedTasksAsync(designerId)).ToList();

            // Assert
            Assert.Single(tasks);
            var t = tasks.First();
            Assert.Equal(od1.OrderDetailId, t.OrderDetailId);
            Assert.Equal("Prod A", t.ProductName);
            Assert.Equal(orderOk.StatusOrder, t.OrderStatus);
            Assert.Equal(ProductionStatus.DESIGNING.ToString(), t.ProductionStatus);
            Assert.NotNull(t.ProductDetails);
            Assert.Equal(v.ProductVariantId.ToString(), t.ProductDetails!.ProductVariantId);
        }

        [Fact]
        public async Task UploadDesignFileAsync_Throws_If_Status_Not_Designing_Or_Redo()
        {
            // Arrange
            var db = CreateDbContext(Guid.NewGuid().ToString());
            var service = CreateService(
                db,
                out var _img,
                out var _notify,
                out var _hub,
                out var _hubClients,
                out var _clientProxy,
                out var _logger);

            string designerId = "designer-1";
            var p = MakeProduct(1, "Prod A");
            var v = MakeVariant(1, p);
            var order = MakeOrder(1, "ORD-1", "seller-1", 3);
            var od = MakeOrderDetail(1, order, v, designerId, true, ProductionStatus.NEED_DESIGN);

            await db.Products.AddAsync(p);
            await db.ProductVariants.AddAsync(v);
            await db.Orders.AddAsync(order);
            await db.SaveChangesAsync();

            var dto = new UploadDesignDto { FileUrl = "https://cdn.example.com/design.psd" };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.UploadDesignFileAsync(od.OrderDetailId, designerId, dto));
        }

        [Fact]
        public async Task UpdateStatusAsync_NeedDesign_To_Designing_Sets_Order_StatusOrder_4_When_All_Detail_Designing()
        {
            // Arrange
            var db = CreateDbContext(Guid.NewGuid().ToString());
            var service = CreateService(
                db,
                out var _img,
                out var _notify,
                out var _hub,
                out var _hubClients,
                out var _clientProxy,
                out var _logger);

            var p = MakeProduct(1, "P");
            var v = MakeVariant(1, p);

            var order = MakeOrder(1, "ORD", "seller", statusOrder: 3); // 3: NEED_DESIGN
            var od1 = MakeOrderDetail(1, order, v, "d1", true, ProductionStatus.NEED_DESIGN);
            var od2 = MakeOrderDetail(2, order, v, "d2", true, ProductionStatus.NEED_DESIGN);

            await db.Products.AddAsync(p);
            await db.ProductVariants.AddAsync(v);
            await db.Orders.AddAsync(order);
            await db.SaveChangesAsync();

            // Act: chuyển od1 -> DESIGNING (chưa all)
            var ok1 = await service.UpdateStatusAsync(od1.OrderDetailId, ProductionStatus.DESIGNING);
            Assert.True(ok1);
            var orderAfter1 = await db.Orders.AsNoTracking().FirstAsync(x => x.OrderId == order.OrderId);
            Assert.Equal(3, orderAfter1.StatusOrder); // chưa lên 4

            // Act: chuyển od2 -> DESIGNING (now all Designing)
            var ok2 = await service.UpdateStatusAsync(od2.OrderDetailId, ProductionStatus.DESIGNING);
            Assert.True(ok2);
            var orderAfter2 = await db.Orders.AsNoTracking().FirstAsync(x => x.OrderId == order.OrderId);
            Assert.Equal(4, orderAfter2.StatusOrder);
        }

        [Fact]
        public async Task UpdateStatusAsync_Invalid_Transition_Throws()
        {
            // Arrange
            var db = CreateDbContext(Guid.NewGuid().ToString());
            var service = CreateService(
                db,
                out var _img,
                out var _notify,
                out var _hub,
                out var _hubClients,
                out var _clientProxy,
                out var _logger);

            var p = MakeProduct(1, "P");
            var v = MakeVariant(1, p);

            var order = MakeOrder(1, "ORD", "seller", statusOrder: 3);
            var od = MakeOrderDetail(1, order, v, "d1", true, ProductionStatus.CHECK_DESIGN);

            await db.Products.AddAsync(p);
            await db.ProductVariants.AddAsync(v);
            await db.Orders.AddAsync(order);
            await db.SaveChangesAsync();

            // Trong code hiện tại, các chuyển đổi hợp lệ:
            // NEED_DESIGN->DESIGNING, DESIGNING/DESIGN_REDO->CHECK_DESIGN, DESIGN_REDO->DESIGNING.
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.UpdateStatusAsync(od.OrderDetailId, ProductionStatus.DESIGN_REDO));
        }

        [Fact]
        public async Task AssignDesignerToOrderDetailAsync_Fails_When_Designer_Not_Allowed()
        {
            // Arrange
            var db = CreateDbContext(Guid.NewGuid().ToString());
            var service = CreateService(
                db,
                out var _img,
                out var _notify,
                out var _hub,
                out var _hubClients,
                out var _clientProxy,
                out var _logger);

            var sellerId = "seller-1";
            var designerId = "designer-1";
            var p = MakeProduct(1, "P");
            var v = MakeVariant(1, p);
            var order = MakeOrder(1, "ORD", sellerId, statusOrder: 2);
            var od = MakeOrderDetail(1, order, v, null, true, ProductionStatus.NEED_DESIGN);

            await db.Products.AddAsync(p);
            await db.ProductVariants.AddAsync(v);
            await db.Orders.AddAsync(order);
            await db.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AssignDesignerToOrderDetailAsync(od.OrderDetailId, designerId, sellerId));
        }

        [Fact]
        public async Task AssignDesignerToOrderAsync_Assigns_All_Details_Sets_NeedDesign_And_Order_Status_3()
        {
            // Arrange
            var db = CreateDbContext(Guid.NewGuid().ToString());
            var service = CreateService(db); // dùng overload không cần out

            var sellerId = "seller-1";
            var designerId = "designer-1";
            var p = MakeProduct(1, "P");
            var v = MakeVariant(1, p);
            var order = MakeOrder(1, "ORD", sellerId, statusOrder: 2);

            var od1 = MakeOrderDetail(1, order, v, null, needDesign: true, prodStatus: null);
            var od2 = MakeOrderDetail(2, order, v, null, needDesign: true, prodStatus: null);

            var ds = new DesignerSeller
            {
                SellerUserId = sellerId,
                DesignerUserId = designerId,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = sellerId
            };

            await db.Products.AddAsync(p);
            await db.ProductVariants.AddAsync(v);
            await db.Orders.AddAsync(order);
            await db.DesignerSellers.AddAsync(ds);
            await db.SaveChangesAsync();

            // Act
            var ok = await service.AssignDesignerToOrderAsync(order.OrderId, designerId, sellerId);

            // Assert
            Assert.True(ok);

            var updatedOrder = await db.Orders.Include(o => o.OrderDetails)
                                              .AsNoTracking()
                                              .FirstAsync(x => x.OrderId == order.OrderId);
            Assert.Equal(3, updatedOrder.StatusOrder); // NEED_DESIGN
            Assert.All(updatedOrder.OrderDetails, d =>
            {
                Assert.Equal(designerId, d.AssignedDesignerUserId);
                Assert.True(d.AssignedAt.HasValue);
                Assert.Equal(ProductionStatus.NEED_DESIGN, d.ProductionStatus);
            });
        }

        [Fact]
        public async Task AssignDesignerToOrderAsync_ReturnsFalse_When_Order_Not_Owned_By_Seller()
        {
            // Arrange
            var db = CreateDbContext(Guid.NewGuid().ToString());
            var service = CreateService(db); // dùng overload không cần out

            var p = MakeProduct(1, "P");
            var v = MakeVariant(1, p);
            var order = MakeOrder(1, "ORD", "seller-OTHER", statusOrder: 2);

            await db.Products.AddAsync(p);
            await db.ProductVariants.AddAsync(v);
            await db.Orders.AddAsync(order);
            await db.SaveChangesAsync();

            // Act
            var ok = await service.AssignDesignerToOrderAsync(order.OrderId, "designer-1", "seller-1");

            // Assert
            Assert.False(ok);
        }
    }
}

