using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CB_Gift.Data;
using CB_Gift.Models;
using CB_Gift.Models.Enums;
using CB_Gift.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CB_Gift.Tests.Services
{
    public class PlanServiceTests
    {
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

        #region Seed helpers
        private static Category Cat(int id, string name) => new Category { CategoryId = id, CategoryName = name };
        private static Product Product(int id, int categoryId) => new Product { ProductId = id, CategoryId = categoryId };
        private static ProductVariant Variant(int id, int productId) => new ProductVariant { ProductVariantId = id, ProductId = productId };
        private static EndCustomer Customer(int id, string name) => new EndCustomer { CustId = id, Name = name };
        private static Order Order(
           int id,
           string code,
           int status,
           int endCustomerId,
           string sellerUserId = "seller-1")  // thêm tham số mặc định
           => new Order
           {
               OrderId = id,
               OrderCode = code,
               StatusOrder = status,
               EndCustomerId = endCustomerId,
               SellerUserId = sellerUserId       // gán field bắt buộc
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
        #endregion

        [Fact]
        public async Task GroupSubmittedOrdersAsync_CreatesPlansPerCategory_AndUpdatesOrderAndDetails()
        {
            using var ctx = NewInMemoryContext(nameof(GroupSubmittedOrdersAsync_CreatesPlansPerCategory_AndUpdatesOrderAndDetails));

            // Categories
            var catA = Cat(1, "Cat A");
            var catB = Cat(2, "Cat B");
            ctx.Categories.AddRange(catA, catB);

            // Products & Variants
            var p1 = Product(1, catA.CategoryId);
            var p2 = Product(2, catB.CategoryId);
            var v1 = Variant(1, p1.ProductId);
            var v2 = Variant(2, p2.ProductId);
            ctx.Products.AddRange(p1, p2);
            ctx.ProductVariants.AddRange(v1, v2);

            // Customers
            var c1 = Customer(1, "Alice");
            var c2 = Customer(2, "Bob");
            ctx.EndCustomers.AddRange(c1, c2);

            // Orders: StatusOrder = 7 (submitted)
            var o1 = Order(1, "O1", 7, c1.CustId);
            var o2 = Order(2, "O2", 7, c2.CustId);
            ctx.Orders.AddRange(o1, o2);

            // OrderDetails (chưa có PlanDetail)
            var od1 = OrderDetail(1, o1.OrderId, v1.ProductVariantId);
            var od2 = OrderDetail(2, o2.OrderId, v2.ProductVariantId);
            ctx.OrderDetails.AddRange(od1, od2);

            await ctx.SaveChangesAsync();

            var svc = new PlanService(ctx);

            // Act
            await svc.GroupSubmittedOrdersAsync("creator-1");

            // Assert
            var plans = await ctx.Plans.Include(p => p.PlanDetails).ToListAsync();
            plans.Should().HaveCount(2);

            var pds = await ctx.PlanDetails.ToListAsync();
            pds.Should().HaveCount(2);
            pds.Should().Contain(pd => pd.OrderDetailId == od1.OrderDetailId);
            pds.Should().Contain(pd => pd.OrderDetailId == od2.OrderDetailId);

            (await ctx.Orders.FindAsync(o1.OrderId))!.StatusOrder.Should().Be(8);
            (await ctx.Orders.FindAsync(o2.OrderId))!.StatusOrder.Should().Be(8);

            (await ctx.OrderDetails.FindAsync(od1.OrderDetailId))!.ProductionStatus.Should().Be(ProductionStatus.READY_PROD);
            (await ctx.OrderDetails.FindAsync(od2.OrderDetailId))!.ProductionStatus.Should().Be(ProductionStatus.READY_PROD);
        }

        [Fact]
        public async Task GetPlansForStaffViewAsync_Filters_Produced()
        {
            using var ctx = NewInMemoryContext(nameof(GetPlansForStaffViewAsync_Filters_Produced));

            var cat = Cat(1, "Cat");
            var prod = Product(1, cat.CategoryId);
            var var1 = Variant(1, prod.ProductId);
            var cus = Customer(1, "Alice");
            var ord = Order(1, "OC1", 8, cus.CustId);
            var od = OrderDetail(1, ord.OrderId, var1.ProductVariantId, qty: 2, prodStatus: ProductionStatus.FINISHED);
            var today = DateTime.UtcNow.Date;
            var plan = Plan(createDate: today, createBy: "u", start: today);
            var pd = new PlanDetail { Plan = plan, OrderDetail = od, StatusOrder = 2, NumberOfFinishedProducts = 2 };

            ctx.AddRange(cat, prod, var1, cus, ord, od, plan, pd);
            await ctx.SaveChangesAsync();

            var svc = new PlanService(ctx);

            var results = (await svc.GetPlansForStaffViewAsync(categoryId: cat.CategoryId, selectedDate: today, status: "produced")).ToList();

            results.Should().HaveCount(1);
            var catGroup = results[0];
            catGroup.CategoryId.Should().Be(cat.CategoryId);
            catGroup.TotalItems.Should().Be(1);
            catGroup.DateGroups.Should().HaveCount(1);
            catGroup.DateGroups[0].Details.Should().HaveCount(1);
            catGroup.DateGroups[0].Details[0].StatusOrder.Should().Be(ProductionStatus.FINISHED);
        }

        [Fact]
        public async Task GetPlansForStaffViewAsync_Filters_NeedsProduction()
        {
            using var ctx = NewInMemoryContext(nameof(GetPlansForStaffViewAsync_Filters_NeedsProduction));

            var cat = Cat(1, "Cat");
            var prod = Product(1, cat.CategoryId);
            var var1 = Variant(1, prod.ProductId);
            var cus = Customer(1, "Bob");
            var ord = Order(1, "OC1", 8, cus.CustId);

            var od1 = OrderDetail(1, ord.OrderId, var1.ProductVariantId, prodStatus: ProductionStatus.READY_PROD);
            var od2 = OrderDetail(2, ord.OrderId, var1.ProductVariantId, prodStatus: ProductionStatus.IN_PROD);
            var od3 = OrderDetail(3, ord.OrderId, var1.ProductVariantId, prodStatus: ProductionStatus.PROD_REWORK);

            var today = DateTime.UtcNow.Date;
            var plan = Plan(createDate: today, createBy: "u", start: today);
            var pd1 = new PlanDetail { Plan = plan, OrderDetail = od1 };
            var pd2 = new PlanDetail { Plan = plan, OrderDetail = od2 };
            var pd3 = new PlanDetail { Plan = plan, OrderDetail = od3 };

            ctx.AddRange(cat, prod, var1, cus, ord, od1, od2, od3, plan, pd1, pd2, pd3);
            await ctx.SaveChangesAsync();

            var svc = new PlanService(ctx);

            var results = (await svc.GetPlansForStaffViewAsync(categoryId: cat.CategoryId, selectedDate: today, status: "needs_production")).ToList();

            results.Should().HaveCount(1);
            results[0].TotalItems.Should().Be(3);
            results[0].DateGroups.Should().ContainSingle();
            results[0].DateGroups[0].ItemCount.Should().Be(3);

            var statuses = results[0].DateGroups[0].Details.Select(d => d.StatusOrder).ToHashSet();
            statuses.Should().Contain(ProductionStatus.READY_PROD);
            statuses.Should().Contain(ProductionStatus.IN_PROD);
            statuses.Should().Contain(ProductionStatus.PROD_REWORK);
        }

        [Fact]
        public async Task GetPlansForStaffViewAsync_Filters_ByCategory_And_Date()
        {
            using var ctx = NewInMemoryContext(nameof(GetPlansForStaffViewAsync_Filters_ByCategory_And_Date));

            var cat1 = Cat(1, "Cat1");
            var cat2 = Cat(2, "Cat2");
            var p1 = Product(1, cat1.CategoryId);
            var p2 = Product(2, cat2.CategoryId);
            var v1 = Variant(1, p1.ProductId);
            var v2 = Variant(2, p2.ProductId);
            var cus = Customer(1, "Alice");
            var o1 = Order(1, "A1", 8, cus.CustId);
            var o2 = Order(2, "A2", 8, cus.CustId);

            var od1 = OrderDetail(1, o1.OrderId, v1.ProductVariantId, prodStatus: ProductionStatus.READY_PROD);
            var od2 = OrderDetail(2, o2.OrderId, v2.ProductVariantId, prodStatus: ProductionStatus.READY_PROD);

            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);

            var plan1 = Plan(createDate: today, createBy: "u1", start: today);
            var plan2 = Plan(createDate: yesterday, createBy: "u2", start: yesterday);

            var pd1 = new PlanDetail { Plan = plan1, OrderDetail = od1 };
            var pd2 = new PlanDetail { Plan = plan2, OrderDetail = od2 };

            ctx.AddRange(cat1, cat2, p1, p2, v1, v2, cus, o1, o2, od1, od2, plan1, plan2, pd1, pd2);
            await ctx.SaveChangesAsync();

            var svc = new PlanService(ctx);

            var results = (await svc.GetPlansForStaffViewAsync(categoryId: cat1.CategoryId, selectedDate: today, status: "needs_production")).ToList();

            results.Should().HaveCount(1);
            results[0].CategoryId.Should().Be(cat1.CategoryId);
            results[0].TotalItems.Should().Be(1);
            results[0].DateGroups.Should().ContainSingle();
            results[0].DateGroups[0].GroupDate.Should().Be(today);
            results[0].DateGroups[0].Details.Should().ContainSingle();

            results.Any(x => x.CategoryId == cat2.CategoryId).Should().BeFalse();
        }

        [Fact]
        public async Task UpdatePlanDetailStatusAsync_UpdatesDetail_AndOrderStatus_UsesMinMapping()
        {
            using var ctx = NewInMemoryContext(nameof(UpdatePlanDetailStatusAsync_UpdatesDetail_AndOrderStatus_UsesMinMapping));

            var cat = Cat(1, "Cat");
            var prod = Product(1, cat.CategoryId);
            var var1 = Variant(1, prod.ProductId);
            var cus = Customer(1, "Alice");
            var ord = Order(1, "OC1", status: 8, endCustomerId: cus.CustId);

            var odFinished = OrderDetail(1, ord.OrderId, var1.ProductVariantId, prodStatus: ProductionStatus.FINISHED);
            var odReady = OrderDetail(2, ord.OrderId, var1.ProductVariantId, prodStatus: ProductionStatus.READY_PROD);

            var today = DateTime.UtcNow.Date;
            var plan = Plan(today, "u", today);
            var pdFinished = new PlanDetail { Plan = plan, OrderDetail = odFinished };
            var pdReady = new PlanDetail { Plan = plan, OrderDetail = odReady };

            ctx.AddRange(cat, prod, var1, cus, ord, odFinished, odReady, plan, pdFinished, pdReady);
            await ctx.SaveChangesAsync();

            var svc = new PlanService(ctx);

            // 1) Đổi pdFinished => IN_PROD
            var ok = await svc.UpdatePlanDetailStatusAsync(pdFinished.PlanDetailId, (int)ProductionStatus.IN_PROD);
            ok.Should().BeTrue();

            (await ctx.OrderDetails.FindAsync(odFinished.OrderDetailId))!.ProductionStatus.Should().Be(ProductionStatus.IN_PROD);
            (await ctx.Orders.FindAsync(ord.OrderId))!.StatusOrder.Should().Be(8); // min(IN_PROD=8, READY_PROD=8) -> map 8

            // 2) Hạ pdReady => HOLD (16). min vẫn 8 => Order.StatusOrder vẫn 8
            ok = await svc.UpdatePlanDetailStatusAsync(pdReady.PlanDetailId, (int)ProductionStatus.HOLD);
            ok.Should().BeTrue();
            (await ctx.Orders.FindAsync(ord.OrderId))!.StatusOrder.Should().Be(8);

            // 3) Nâng cả 2 => FINISHED (10) -> min = 10 => map = 10
            ok = await svc.UpdatePlanDetailStatusAsync(pdReady.PlanDetailId, (int)ProductionStatus.FINISHED);
            ok.Should().BeTrue();
            ok = await svc.UpdatePlanDetailStatusAsync(pdFinished.PlanDetailId, (int)ProductionStatus.FINISHED);
            ok.Should().BeTrue();

            (await ctx.Orders.FindAsync(ord.OrderId))!.StatusOrder.Should().Be(10);
        }

        [Fact]
        public async Task UpdatePlanDetailStatusAsync_ReturnsFalse_WhenPlanDetailMissing()
        {
            using var ctx = NewInMemoryContext(nameof(UpdatePlanDetailStatusAsync_ReturnsFalse_WhenPlanDetailMissing));
            var svc = new PlanService(ctx);

            var ok = await svc.UpdatePlanDetailStatusAsync(999, (int)ProductionStatus.IN_PROD);
            ok.Should().BeFalse();
        }
    }
}
