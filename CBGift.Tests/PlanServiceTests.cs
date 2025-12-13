using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CB_Gift.Data;
using CB_Gift.Models;
using CB_Gift.Models.Enums;
using CB_Gift.Services;
using CB_Gift.Services.IService;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using static CB_Gift.DTOs.StaffViewDtos;

namespace CB_Gift.Tests.Services
{
    public class PlanServiceTests
    {
        // ---------------------------
        // InMemory DbContext factory
        // ---------------------------
        private static CBGiftDbContext NewInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<CBGiftDbContext>()
                .UseInMemoryDatabase(dbName)
                .EnableSensitiveDataLogging()
                .Options;

            var ctx = new CBGiftDbContext(options);
            ctx.Database.EnsureCreated();
            return ctx;
        }

        // ---------------------------
        // UserManager mock helper
        // ---------------------------
        private static Mock<UserManager<AppUser>> BuildUserManagerMock()
        {
            var store = new Mock<IUserStore<AppUser>>();
            return new Mock<UserManager<AppUser>>(
                store.Object,
                null, null, null, null, null, null, null, null
            );
        }

        // ---------------------------
        // Seed helpers
        // ---------------------------
        private static Category Cat(int id, string name) => new Category { CategoryId = id, CategoryName = name };

        private static Product Product(int id, int categoryId, string name = "P")
            => new Product { ProductId = id, CategoryId = categoryId, ProductName = name };

        private static ProductVariant Variant(int id, int productId, string sku = "SKU")
            => new ProductVariant { ProductVariantId = id, ProductId = productId, Sku = sku };

        private static EndCustomer Customer(int id, string name)
            => new EndCustomer { CustId = id, Name = name, Phone = "0123", Email = "a@a.com", Address = "addr" };

        private static Order Order(
            int id,
            string code,
            int status,
            int endCustomerId,
            string sellerUserId = "seller-1")
            => new Order
            {
                OrderId = id,
                OrderCode = code,
                StatusOrder = status,
                EndCustomerId = endCustomerId,
                SellerUserId = sellerUserId,
                OrderDate = DateTime.UtcNow,
                CreationDate = DateTime.UtcNow
            };

        private static OrderDetail OrderDetail(
            int id, int orderId, int variantId, int qty = 1,
            ProductionStatus? prodStatus = null,
            string linkImg = "img", string note = "note", string file = "file", string thanks = "thanks")
            => new OrderDetail
            {
                OrderDetailId = id,
                OrderId = orderId,
                ProductVariantId = variantId,
                Quantity = qty,
                ProductionStatus = prodStatus,
                LinkImg = linkImg,
                Note = note,
                LinkFileDesign = file,
                LinkThanksCard = thanks
            };

        private static Plan Plan(DateTime? createDate = null, string createBy = "u1", DateTime? start = null)
            => new Plan
            {
                CreateDate = createDate ?? DateTime.UtcNow,
                CreateByUserId = createBy,
                StartDatePlan = start ?? DateTime.UtcNow
            };

        // ---------------------------
        // Service builder
        // ---------------------------
        private static PlanService BuildService(
            CBGiftDbContext ctx,
            Mock<INotificationService>? notifyMock = null,
            Mock<UserManager<AppUser>>? userManagerMock = null,
            Mock<ILogger<PlanService>>? loggerMock = null)
        {
            notifyMock ??= new Mock<INotificationService>(MockBehavior.Strict);
            notifyMock
                .Setup(n => n.CreateAndSendNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            userManagerMock ??= BuildUserManagerMock();
            loggerMock ??= new Mock<ILogger<PlanService>>();

            return new PlanService(loggerMock.Object, ctx, notifyMock.Object, userManagerMock.Object);
        }

        // =========================================================
        // 1) GroupSubmittedOrdersAsync
        // =========================================================

        [Fact]
        public async Task GroupSubmittedOrdersAsync_CreatesPlansPerCategory_AndUpdatesOrderAndDetails_AndNotifiesStaff()
        {
            using var ctx = NewInMemoryContext(nameof(GroupSubmittedOrdersAsync_CreatesPlansPerCategory_AndUpdatesOrderAndDetails_AndNotifiesStaff));

            // Arrange
            var notifyMock = new Mock<INotificationService>(MockBehavior.Strict);
            notifyMock
                .Setup(n => n.CreateAndSendNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var userManagerMock = BuildUserManagerMock();
            userManagerMock
                .Setup(um => um.GetUsersInRoleAsync("Staff"))
                .ReturnsAsync(new List<AppUser>
                {
                    new AppUser { Id = "staff-1" },
                    new AppUser { Id = "staff-2" }
                });

            var svc = BuildService(ctx, notifyMock, userManagerMock);

            // Categories
            var catA = Cat(1, "Cat A");
            var catB = Cat(2, "Cat B");
            ctx.Categories.AddRange(catA, catB);

            // Products & Variants
            var p1 = Product(1, catA.CategoryId, "P1");
            var p2 = Product(2, catB.CategoryId, "P2");
            var v1 = Variant(1, p1.ProductId, "SKU-1");
            var v2 = Variant(2, p2.ProductId, "SKU-2");
            ctx.Products.AddRange(p1, p2);
            ctx.ProductVariants.AddRange(v1, v2);

            // Customers
            var c1 = Customer(1, "Alice");
            var c2 = Customer(2, "Bob");
            ctx.EndCustomers.AddRange(c1, c2);

            // Orders: StatusOrder = 7 (submitted)
            var o1 = Order(1, "O1", 7, c1.CustId, sellerUserId: "seller-1");
            var o2 = Order(2, "O2", 7, c2.CustId, sellerUserId: "seller-2");
            ctx.Orders.AddRange(o1, o2);

            // OrderDetails
            var od1 = OrderDetail(1, o1.OrderId, v1.ProductVariantId, prodStatus: ProductionStatus.READY_PROD);
            var od2 = OrderDetail(2, o2.OrderId, v2.ProductVariantId, prodStatus: ProductionStatus.READY_PROD);
            ctx.OrderDetails.AddRange(od1, od2);

            await ctx.SaveChangesAsync();

            // Act
            await svc.GroupSubmittedOrdersAsync("creator-1");

            // Assert: 2 categories => 2 plans
            var plans = await ctx.Plans.Include(p => p.PlanDetails).ToListAsync();
            plans.Should().HaveCount(2);

            var pds = await ctx.PlanDetails.ToListAsync();
            pds.Should().HaveCount(2);
            pds.Should().Contain(pd => pd.OrderDetailId == od1.OrderDetailId);
            pds.Should().Contain(pd => pd.OrderDetailId == od2.OrderDetailId);

            // Orders updated to 8
            (await ctx.Orders.FindAsync(o1.OrderId))!.StatusOrder.Should().Be(8);
            (await ctx.Orders.FindAsync(o2.OrderId))!.StatusOrder.Should().Be(8);

            // Details updated to READY_PROD
            (await ctx.OrderDetails.FindAsync(od1.OrderDetailId))!.ProductionStatus.Should().Be(ProductionStatus.READY_PROD);
            (await ctx.OrderDetails.FindAsync(od2.OrderDetailId))!.ProductionStatus.Should().Be(ProductionStatus.READY_PROD);

            // Notify staff: called for each staff
            notifyMock.Verify(n => n.CreateAndSendNotificationAsync(
                "staff-1",
                It.Is<string>(msg => msg.Contains("kế hoạch sản xuất", StringComparison.OrdinalIgnoreCase)),
                It.Is<string>(link => link == "/staff/needs-production")
            ), Times.Once);

            notifyMock.Verify(n => n.CreateAndSendNotificationAsync(
                "staff-2",
                It.IsAny<string>(),
                It.IsAny<string>()
            ), Times.Once);
        }

        [Fact]
        public async Task GroupSubmittedOrdersAsync_Skips_When_No_Submitted_OrderDetails()
        {
            using var ctx = NewInMemoryContext(nameof(GroupSubmittedOrdersAsync_Skips_When_No_Submitted_OrderDetails));

            var notifyMock = new Mock<INotificationService>(MockBehavior.Strict);
            notifyMock
                .Setup(n => n.CreateAndSendNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var userManagerMock = BuildUserManagerMock();
            userManagerMock
                .Setup(um => um.GetUsersInRoleAsync("Staff"))
                .ReturnsAsync(new List<AppUser> { new AppUser { Id = "staff-1" } });

            var svc = BuildService(ctx, notifyMock, userManagerMock);

            // No data
            await svc.GroupSubmittedOrdersAsync("creator-1");

            (await ctx.Plans.CountAsync()).Should().Be(0);
            (await ctx.PlanDetails.CountAsync()).Should().Be(0);

            // No plans => no notification should be sent
            notifyMock.Verify(n => n.CreateAndSendNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GroupSubmittedOrdersAsync_DoesNotDuplicatePlanDetails_When_AlreadyGrouped()
        {
            using var ctx = NewInMemoryContext(nameof(GroupSubmittedOrdersAsync_DoesNotDuplicatePlanDetails_When_AlreadyGrouped));

            var svc = BuildService(ctx);

            var cat = Cat(1, "Cat");
            var p = Product(1, cat.CategoryId, "P");
            var v = Variant(1, p.ProductId, "SKU");
            var cus = Customer(1, "Alice");
            var ord = Order(1, "O1", 7, cus.CustId);

            var od = OrderDetail(1, ord.OrderId, v.ProductVariantId, prodStatus: ProductionStatus.READY_PROD);

            var plan = Plan();
            var existingPd = new PlanDetail { Plan = plan, OrderDetailId = od.OrderDetailId };

            ctx.AddRange(cat, p, v, cus, ord, od, plan, existingPd);
            await ctx.SaveChangesAsync();

            await svc.GroupSubmittedOrdersAsync("creator-1");

            // Still 1 PlanDetail, not duplicated
            (await ctx.PlanDetails.CountAsync()).Should().Be(1);
        }

        // =========================================================
        // 2) GetPlansForStaffViewAsync
        // =========================================================

        [Fact]
        public async Task GetPlansForStaffViewAsync_Filters_Produced()
        {
            using var ctx = NewInMemoryContext(nameof(GetPlansForStaffViewAsync_Filters_Produced));

            var svc = BuildService(ctx);

            var cat = Cat(1, "Cat");
            var prod = Product(1, cat.CategoryId, "P");
            var var1 = Variant(1, prod.ProductId, "SKU-1");
            var cus = Customer(1, "Alice");
            var ord = Order(1, "OC1", 8, cus.CustId);

            var od = OrderDetail(1, ord.OrderId, var1.ProductVariantId, qty: 2, prodStatus: ProductionStatus.FINISHED);

            var today = DateTime.UtcNow.Date;
            var plan = Plan(createDate: today, createBy: "u", start: today);
            var pd = new PlanDetail { Plan = plan, OrderDetail = od, StatusOrder = 2, NumberOfFinishedProducts = 2 };

            ctx.AddRange(cat, prod, var1, cus, ord, od, plan, pd);
            await ctx.SaveChangesAsync();

            var results = (await svc.GetPlansForStaffViewAsync(categoryId: cat.CategoryId, selectedDate: today, status: "produced")).ToList();

            results.Should().HaveCount(1);
            results[0].CategoryId.Should().Be(cat.CategoryId);
            results[0].TotalItems.Should().Be(1);
            results[0].DateGroups.Should().HaveCount(1);
            results[0].DateGroups[0].OrderGroups.Should().HaveCount(1);
            results[0].DateGroups[0].OrderGroups[0].Details.Should().HaveCount(1);
            results[0].DateGroups[0].OrderGroups[0].Details[0].StatusOrder.Should().Be(ProductionStatus.FINISHED);
        }

        [Fact]
        public async Task GetPlansForStaffViewAsync_Filters_NeedsProduction_Includes_Ready_InProd_QcFail()
        {
            using var ctx = NewInMemoryContext(nameof(GetPlansForStaffViewAsync_Filters_NeedsProduction_Includes_Ready_InProd_QcFail));

            var svc = BuildService(ctx);

            var cat = Cat(1, "Cat");
            var prod = Product(1, cat.CategoryId, "P");
            var var1 = Variant(1, prod.ProductId, "SKU-1");
            var cus = Customer(1, "Bob");
            var ord = Order(1, "OC1", 8, cus.CustId);

            var od1 = OrderDetail(1, ord.OrderId, var1.ProductVariantId, prodStatus: ProductionStatus.READY_PROD);
            var od2 = OrderDetail(2, ord.OrderId, var1.ProductVariantId, prodStatus: ProductionStatus.IN_PROD);
            var od3 = OrderDetail(3, ord.OrderId, var1.ProductVariantId, prodStatus: ProductionStatus.QC_FAIL);

            var today = DateTime.UtcNow.Date;
            var plan = Plan(createDate: today, createBy: "u", start: today);

            var pd1 = new PlanDetail { Plan = plan, OrderDetail = od1 };
            var pd2 = new PlanDetail { Plan = plan, OrderDetail = od2 };
            var pd3 = new PlanDetail { Plan = plan, OrderDetail = od3 };

            ctx.AddRange(cat, prod, var1, cus, ord, od1, od2, od3, plan, pd1, pd2, pd3);
            await ctx.SaveChangesAsync();

            var results = (await svc.GetPlansForStaffViewAsync(categoryId: cat.CategoryId, selectedDate: today, status: "needs_production")).ToList();

            results.Should().HaveCount(1);
            results[0].TotalItems.Should().Be(3);

            var allStatuses = results[0].DateGroups
                .SelectMany(g => g.OrderGroups)
                .SelectMany(og => og.Details)
                .Select(d => d.StatusOrder)
                .ToHashSet();

            allStatuses.Should().Contain(ProductionStatus.READY_PROD);
            allStatuses.Should().Contain(ProductionStatus.IN_PROD);
            allStatuses.Should().Contain(ProductionStatus.QC_FAIL);
        }

        [Fact]
        public async Task GetPlansForStaffViewAsync_ReturnsEmpty_When_Status_Invalid()
        {
            using var ctx = NewInMemoryContext(nameof(GetPlansForStaffViewAsync_ReturnsEmpty_When_Status_Invalid));
            var svc = BuildService(ctx);

            var res = await svc.GetPlansForStaffViewAsync(categoryId: null, selectedDate: null, status: "wrong_status");
            res.Should().NotBeNull();
            res.Should().BeEmpty();
        }

        [Fact]
        public async Task GetPlansForStaffViewAsync_Populates_Reason_From_OrderDetailLogs_For_QcFail_Or_ProdRework()
        {
            using var ctx = NewInMemoryContext(nameof(GetPlansForStaffViewAsync_Populates_Reason_From_OrderDetailLogs_For_QcFail_Or_ProdRework));
            var svc = BuildService(ctx);

            var cat = Cat(1, "Cat");
            var prod = Product(1, cat.CategoryId, "P");
            var var1 = Variant(1, prod.ProductId, "SKU-1");
            var cus = Customer(1, "Alice");
            var ord = Order(1, "OC1", 8, cus.CustId);

            // QC_FAIL to trigger reason
            var od = OrderDetail(1, ord.OrderId, var1.ProductVariantId, prodStatus: ProductionStatus.QC_FAIL);

            var today = DateTime.UtcNow.Date;
            var plan = Plan(createDate: today, createBy: "u", start: today);
            var pd = new PlanDetail { Plan = plan, OrderDetail = od };

            var log = new OrderDetailLog
            {
                OrderDetailId = od.OrderDetailId,
                ActorUserId = "qc-1",
                EventType = "QC_REJECTED",
                Reason = "Image is blurred",
                CreatedAt = DateTime.UtcNow
            };

            ctx.AddRange(cat, prod, var1, cus, ord, od, plan, pd, log);
            await ctx.SaveChangesAsync();

            // needs_production includes QC_FAIL
            var results = (await svc.GetPlansForStaffViewAsync(categoryId: cat.CategoryId, selectedDate: today, status: "needs_production")).ToList();
            results.Should().HaveCount(1);

            var detail = results[0].DateGroups
                .SelectMany(g => g.OrderGroups)
                .SelectMany(og => og.Details)
                .Single();

            detail.Reason.Should().Be("Image is blurred");
        }

        // =========================================================
        // 3) UpdatePlanDetailStatusAsync
        // =========================================================

        [Fact]
        public async Task UpdatePlanDetailStatusAsync_ReturnsFalse_WhenPlanDetailMissing()
        {
            using var ctx = NewInMemoryContext(nameof(UpdatePlanDetailStatusAsync_ReturnsFalse_WhenPlanDetailMissing));
            var svc = BuildService(ctx);

            var ok = await svc.UpdatePlanDetailStatusAsync(999, (int)ProductionStatus.IN_PROD);
            ok.Should().BeFalse();
        }

        [Fact]
        public async Task UpdatePlanDetailStatusAsync_Updates_OrderDetail_And_OrderStatus_ByMinProductionStatus()
        {
            using var ctx = NewInMemoryContext(nameof(UpdatePlanDetailStatusAsync_Updates_OrderDetail_And_OrderStatus_ByMinProductionStatus));
            var svc = BuildService(ctx);

            var cat = Cat(1, "Cat");
            var prod = Product(1, cat.CategoryId, "P");
            var var1 = Variant(1, prod.ProductId, "SKU-1");
            var cus = Customer(1, "Alice");
            var ord = Order(1, "OC1", status: 8, endCustomerId: cus.CustId, sellerUserId: "seller-1");

            // Two details
            var od1 = OrderDetail(1, ord.OrderId, var1.ProductVariantId, prodStatus: ProductionStatus.READY_PROD);
            var od2 = OrderDetail(2, ord.OrderId, var1.ProductVariantId, prodStatus: ProductionStatus.FINISHED);

            var plan = Plan();
            var pd1 = new PlanDetail { Plan = plan, OrderDetail = od1 };
            var pd2 = new PlanDetail { Plan = plan, OrderDetail = od2 };

            ctx.AddRange(cat, prod, var1, cus, ord, od1, od2, plan, pd1, pd2);
            await ctx.SaveChangesAsync();

            // Act: change od2 from FINISHED -> QC_DONE (status 11 => Order.StatusOrder should map 8? depends on min across details)
            var ok = await svc.UpdatePlanDetailStatusAsync(pd2.PlanDetailId, (int)ProductionStatus.QC_DONE);
            ok.Should().BeTrue();

            var updatedOd2 = await ctx.OrderDetails.FindAsync(od2.OrderDetailId);
            updatedOd2!.ProductionStatus.Should().Be(ProductionStatus.QC_DONE);

            // min between READY_PROD (8) and QC_DONE (11) => READY_PROD => Map => 8
            (await ctx.Orders.FindAsync(ord.OrderId))!.StatusOrder.Should().Be(8);
        }

        [Fact]
        public async Task UpdatePlanDetailStatusAsync_Sends_Seller_Notification_For_Status8_And_Sends_QC_Notifications()
        {
            using var ctx = NewInMemoryContext(nameof(UpdatePlanDetailStatusAsync_Sends_Seller_Notification_For_Status8_And_Sends_QC_Notifications));

            var notifyMock = new Mock<INotificationService>(MockBehavior.Strict);
            notifyMock
                .Setup(n => n.CreateAndSendNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var userManagerMock = BuildUserManagerMock();
            userManagerMock
                .Setup(um => um.GetUsersInRoleAsync("QC"))
                .ReturnsAsync(new List<AppUser>
                {
                    new AppUser { Id = "qc-1" },
                    new AppUser { Id = "qc-2" }
                });

            var svc = BuildService(ctx, notifyMock, userManagerMock);

            var cat = Cat(1, "Cat");
            var prod = Product(1, cat.CategoryId, "Mug");
            var var1 = Variant(1, prod.ProductId, "SKU-1");
            var cus = Customer(1, "Alice");
            var ord = Order(1, "OC1", status: 8, endCustomerId: cus.CustId, sellerUserId: "seller-1");

            var od = OrderDetail(1, ord.OrderId, var1.ProductVariantId, prodStatus: ProductionStatus.IN_PROD);
            var plan = Plan();
            var pd = new PlanDetail { Plan = plan, OrderDetail = od };

            ctx.AddRange(cat, prod, var1, cus, ord, od, plan, pd);
            await ctx.SaveChangesAsync();

            // newStatus == 8 triggers: seller message + QC notify loop (per your code)
            var ok = await svc.UpdatePlanDetailStatusAsync(pd.PlanDetailId, 8);
            ok.Should().BeTrue();

            // Verify Seller notification
            notifyMock.Verify(n => n.CreateAndSendNotificationAsync(
                "seller-1",
                It.Is<string>(msg => msg.Contains("đã sản xuất xong", StringComparison.OrdinalIgnoreCase)
                                  && msg.Contains("OC1")),
                It.Is<string>(link => link == "/seller/orders/1")
            ), Times.Once);

            // Verify QC notifications
            notifyMock.Verify(n => n.CreateAndSendNotificationAsync(
                "qc-1",
                It.Is<string>(msg => msg.Contains("Yêu cầu kiểm tra", StringComparison.OrdinalIgnoreCase)),
                It.Is<string>(link => link == "/qc/check-product")
            ), Times.Once);

            notifyMock.Verify(n => n.CreateAndSendNotificationAsync(
                "qc-2",
                It.IsAny<string>(),
                It.IsAny<string>()
            ), Times.Once);
        }
    }
}
