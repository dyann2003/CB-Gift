using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Models;
using CB_Gift.Models.Enums;
using CB_Gift.Services;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
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

        private static DesignerTaskService CreateService(CBGiftDbContext ctx)
        {
            var imageSvcMock = new Mock<IImageManagementService>(MockBehavior.Strict);
            // Không dùng nhánh upload file trong test, nên không cần Setup.
            return new DesignerTaskService(ctx, imageSvcMock.Object);
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
                LengthCm = 10m,      // decimal
                HeightCm = 20m,      // decimal
                WidthCm = 5m,       // decimal
                ThicknessMm = "2",   // string
                SizeInch = "8x10", // string
                Layer = "1",    // string
                CustomShape = "false", // string
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
            string designerId,
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
            var service = CreateService(db);

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
            Assert.Equal(v.ProductVariantId.ToString(), t.ProductDetails.ProductVariantId);
        }

        //[Fact]
        //public async Task UploadDesignFileAsync_With_FileUrl_Updates_Record_And_Moves_To_CHECK_DESIGN()
        //{
        //    // Arrange
        //    var db = CreateDbContext(Guid.NewGuid().ToString());
        //    var service = CreateService(db);

        //    string designerId = "designer-1";
        //    var p = MakeProduct(1, "Prod A");
        //    var v = MakeVariant(1, p);
        //    var order = MakeOrder(1, "ORD-1", "seller-1", 3);
        //    var od = MakeOrderDetail(1, order, v, designerId, true, ProductionStatus.DESIGNING);

        //    await db.Products.AddAsync(p);
        //    await db.ProductVariants.AddAsync(v);
        //    await db.Orders.AddAsync(order);
        //    await db.SaveChangesAsync();

        //    var dto = new UploadDesignDto
        //    {
        //        // Không nạp file; đi theo nhánh FileUrl
        //        FileUrl = "https://cdn.example.com/design.psd",
        //        Note = "v1-preview"
        //    };

        //    // Act
        //    var ok = await service.UploadDesignFileAsync(od.OrderDetailId, designerId, dto);

        //    // Assert
        //    Assert.True(ok);

        //    var updated = await db.OrderDetails.AsNoTracking().FirstAsync(x => x.OrderDetailId == od.OrderDetailId);
        //    Assert.Equal("https://cdn.example.com/design.psd", updated.LinkFileDesign);
        //    Assert.Equal(ProductionStatus.CHECK_DESIGN, updated.ProductionStatus);

        //    var designRow = await db.OrderDetailDesigns.AsNoTracking().FirstOrDefaultAsync(x => x.OrderDetailId == od.OrderDetailId);
        //    Assert.NotNull(designRow);
        //    Assert.False(designRow!.IsFinal);
        //    Assert.Equal(dto.Note, designRow.Note);
        //    Assert.Equal(dto.FileUrl, designRow.FileUrl);
        //}

        [Fact]
        public async Task UploadDesignFileAsync_Throws_If_Status_Not_Designing_Or_Redo()
        {
            // Arrange
            var db = CreateDbContext(Guid.NewGuid().ToString());
            var service = CreateService(db);

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

        //[Fact]
        //public async Task UpdateStatusAsync_Designing_To_CheckDesign_Also_Sets_Order_StatusOrder_5_When_All_Checked()
        //{
        //    // Arrange
        //    var db = CreateDbContext(Guid.NewGuid().ToString());
        //    var service = CreateService(db);

        //    var p = MakeProduct(1, "P");
        //    var v = MakeVariant(1, p);

        //    var order = MakeOrder(1, "ORD", "seller", statusOrder: 4); // đang ở 4 (ví dụ: All Designing)
        //    var od1 = MakeOrderDetail(1, order, v, "d1", true, ProductionStatus.DESIGNING);
        //    var od2 = MakeOrderDetail(2, order, v, "d2", true, ProductionStatus.DESIGNING);

        //    await db.Products.AddAsync(p);
        //    await db.ProductVariants.AddAsync(v);
        //    await db.Orders.AddAsync(order);
        //    await db.SaveChangesAsync();

        //    // Act: chuyển od1 -> CHECK_DESIGN (chưa all)
        //    var ok1 = await service.UpdateStatusAsync(od1.OrderDetailId, ProductionStatus.CHECK_DESIGN);
        //    Assert.True(ok1);

        //    var orderAfter1 = await db.Orders.AsNoTracking().FirstAsync(x => x.OrderId == order.OrderId);
        //    Assert.Equal(4, orderAfter1.StatusOrder); // chưa lên 5 vì od2 chưa CHECK_DESIGN

        //    // Act: chuyển od2 -> CHECK_DESIGN (now all)
        //    var ok2 = await service.UpdateStatusAsync(od2.OrderDetailId, ProductionStatus.CHECK_DESIGN);
        //    Assert.True(ok2);

        //    var orderAfter2 = await db.Orders.AsNoTracking().FirstAsync(x => x.OrderId == order.OrderId);
        //    Assert.Equal(5, orderAfter2.StatusOrder); // đã lên 5 khi tất cả đều CHECK_DESIGN
        //}

        [Fact]
        public async Task UpdateStatusAsync_NeedDesign_To_Designing_Sets_Order_StatusOrder_4_When_All_Detail_Designing()
        {
            // Arrange
            var db = CreateDbContext(Guid.NewGuid().ToString());
            var service = CreateService(db);

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
            var service = CreateService(db);

            var p = MakeProduct(1, "P");
            var v = MakeVariant(1, p);

            var order = MakeOrder(1, "ORD", "seller", statusOrder: 3);
            var od = MakeOrderDetail(1, order, v, "d1", true, ProductionStatus.CHECK_DESIGN);

            await db.Products.AddAsync(p);
            await db.ProductVariants.AddAsync(v);
            await db.Orders.AddAsync(order);
            await db.SaveChangesAsync();

            // CHECK_DESIGN -> DESIGN_REDO là hợp lệ theo business?
            // Trong code hiện tại, chỉ cho: DESIGN_REDO->DESIGNING, NEED_DESIGN->DESIGNING, DESIGNING->CHECK_DESIGN.
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.UpdateStatusAsync(od.OrderDetailId, ProductionStatus.DESIGN_REDO));
        }

        //[Fact]
        //public async Task AssignDesignerToOrderDetailAsync_Succeeds_When_SellerOwns_And_DesignerAllowed()
        //{
        //    // Arrange
        //    var db = CreateDbContext(Guid.NewGuid().ToString());
        //    var service = CreateService(db);

        //    var sellerId = "seller-1";
        //    var designerId = "designer-1";
        //    var p = MakeProduct(1, "P");
        //    var v = MakeVariant(1, p);
        //    var order = MakeOrder(1, "ORD", sellerId, statusOrder: 2); // 2: Lên Đơn
        //    var od = MakeOrderDetail(1, order, v, null, true, ProductionStatus.NEED_DESIGN);

        //    // Quan hệ Designer–Seller cho phép
        //    var ds = new DesignerSeller
        //    {
        //        SellerUserId = sellerId,
        //        DesignerUserId = designerId,
        //        CreatedAt = DateTime.UtcNow,
        //        CreatedByUserId = sellerId
        //    };

        //    await db.Products.AddAsync(p);
        //    await db.ProductVariants.AddAsync(v);
        //    await db.Orders.AddAsync(order);
        //    await db.DesignerSellers.AddAsync(ds);
        //    await db.SaveChangesAsync();

        //    // Act
        //    var ok = await service.AssignDesignerToOrderDetailAsync(od.OrderDetailId, designerId, sellerId);

        //    // Assert
        //    Assert.True(ok);

        //    var updatedOd = await db.OrderDetails.AsNoTracking().FirstAsync(x => x.OrderDetailId == od.OrderDetailId);
        //    Assert.Equal(designerId, updatedOd.AssignedDesignerUserId);
        //    Assert.True(updatedOd.AssignedAt.HasValue);
        //    var updatedOrder = await db.Orders.AsNoTracking().FirstAsync(x => x.OrderId == order.OrderId);
        //    Assert.Equal(3, updatedOrder.StatusOrder); // chuyển lên NEED_DESIGN
        //}

        [Fact]
        public async Task AssignDesignerToOrderDetailAsync_Fails_When_Designer_Not_Allowed()
        {
            // Arrange
            var db = CreateDbContext(Guid.NewGuid().ToString());
            var service = CreateService(db);

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

        //[Fact]
        //public async Task AssignDesignerToOrderAsync_Assigns_All_Details_Sets_NeedDesign_And_Order_Status_3()
        //{
        //    // Arrange
        //    var db = CreateDbContext(Guid.NewGuid().ToString());
        //    var service = CreateService(db);

        //    var sellerId = "seller-1";
        //    var designerId = "designer-1";
        //    var p = MakeProduct(1, "P");
        //    var v = MakeVariant(1, p);
        //    var order = MakeOrder(1, "ORD", sellerId, statusOrder: 2);

        //    var od1 = MakeOrderDetail(1, order, v, null, needDesign: true, prodStatus: null);
        //    var od2 = MakeOrderDetail(2, order, v, null, needDesign: true, prodStatus: null);

        //    var ds = new DesignerSeller
        //    {
        //        SellerUserId = sellerId,
        //        DesignerUserId = designerId,
        //        CreatedAt = DateTime.UtcNow,
        //        CreatedByUserId = sellerId
        //    };

        //    await db.Products.AddAsync(p);
        //    await db.ProductVariants.AddAsync(v);
        //    await db.Orders.AddAsync(order);
        //    await db.DesignerSellers.AddAsync(ds);
        //    await db.SaveChangesAsync();

        //    // Act
        //    var ok = await service.AssignDesignerToOrderAsync(order.OrderId, designerId, sellerId);

        //    // Assert
        //    Assert.True(ok);

        //    var updatedOrder = await db.Orders.Include(o => o.OrderDetails)
        //                                      .AsNoTracking()
        //                                      .FirstAsync(x => x.OrderId == order.OrderId);
        //    Assert.Equal(3, updatedOrder.StatusOrder); // NEED_DESIGN
        //    Assert.All(updatedOrder.OrderDetails, d =>
        //    {
        //        Assert.Equal(designerId, d.AssignedDesignerUserId);
        //        Assert.True(d.AssignedAt.HasValue);
        //        Assert.Equal(ProductionStatus.NEED_DESIGN, d.ProductionStatus);
        //    });
        //}

        //[Fact]
        //public async Task AssignDesignerToOrderAsync_ReturnsFalse_When_Order_Not_Owned_By_Seller()
        //{
        //    // Arrange
        //    var db = CreateDbContext(Guid.NewGuid().ToString());
        //    var service = CreateService(db);

        //    var p = MakeProduct(1, "P");
        //    var v = MakeVariant(1, p);
        //    var order = MakeOrder(1, "ORD", "seller-OTHER", statusOrder: 2);

        //    await db.Products.AddAsync(p);
        //    await db.ProductVariants.AddAsync(v);
        //    await db.Orders.AddAsync(order);
        //    await db.SaveChangesAsync();

        //    // Act
        //    var ok = await service.AssignDesignerToOrderAsync(order.OrderId, "designer-1", "seller-1");

        //    // Assert
        //    Assert.False(ok);
        //}
    }
}
