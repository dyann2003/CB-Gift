using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Models;
using CB_Gift.Models.Enums;
using CB_Gift.Services;
using CB_Gift.Services.IService;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
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

        private static Mock<UserManager<AppUser>> CreateUserManagerMock()
        {
            var store = new Mock<IUserStore<AppUser>>();
            return new Mock<UserManager<AppUser>>(
                store.Object, null, null, null, null, null, null, null, null);
        }

        private static object? GetProp(object obj, string propName)
            => obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(obj);

        private static async Task<(CBGiftDbContext db, OrderDetailService svc, int od1, int od2, int orderId, Mock<UserManager<AppUser>> um, Mock<INotificationService> notif)> SeedAsync()
        {
            var db = NewDb();

            var endCustomer = new EndCustomer { CustId = 1, Name = "Customer A" };
            var product = new Product { ProductId = 1, ProductName = "Mug", ProductCode = "P001", CategoryId = 1, Status = 1, Describe = "desc" };
            var variant = new ProductVariant
            {
                ProductVariantId = 10,
                ProductId = 1,
                Product = product,
                Sku = "MUG-RED",

                SizeInch = "12",
                ThicknessMm = "2",
                Layer = "1",
                CustomShape = "false",

                LengthCm = 10m,
                HeightCm = 10m,
                WidthCm = 10m,
                WeightGram = 100m,
                BaseCost = 10m,
                ShipCost = 1m,
                ExtraShipping = 0m,
                TotalCost = 11m
            };
            var orderStatus = new OrderStatus { StatusId = 1, Code = "CREATED", NameVi = "Đơn mới tạo" };

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
                Quantity = 2,
                ProductionStatus = ProductionStatus.CREATED
            };

            var odB = new OrderDetail
            {
                OrderDetailId = 1002,
                OrderId = 100,
                Order = order,
                ProductVariantId = 10,
                ProductVariant = variant,
                Quantity = 1,
                ProductionStatus = ProductionStatus.DESIGNING
            };

            db.EndCustomers.Add(endCustomer);
            db.Products.Add(product);
            db.ProductVariants.Add(variant);
            db.OrderStatuses.Add(orderStatus);
            db.Orders.Add(order);
            db.OrderDetails.AddRange(odA, odB);

            await db.SaveChangesAsync();

            var um = CreateUserManagerMock();
            var notif = new Mock<INotificationService>();

            var svc = new OrderDetailService(db, um.Object, notif.Object);

            return (db, svc, odA.OrderDetailId, odB.OrderDetailId, order.OrderId, um, notif);
        }

        [Fact]
        public async Task GetOrderDetailByIdAsync_Returns_ProductVariant_Product_TotalItems_ItemIndex()
        {
            var (db, svc, od1, od2, _, _, _) = await SeedAsync();

            var d1 = await svc.GetOrderDetailByIdAsync(od1);
            d1.Should().NotBeNull();

            var pv1 = GetProp(d1!, "ProductVariant");
            pv1.Should().NotBeNull();

            var prod1 = GetProp(pv1!, "Product");
            prod1.Should().NotBeNull();

            GetProp(prod1!, "ProductName")!.Should().Be("Mug");
            GetProp(d1!, "TotalItems")!.Should().Be(3);
            GetProp(d1!, "ItemIndex")!.Should().Be(1);

            var d2 = await svc.GetOrderDetailByIdAsync(od2);
            d2.Should().NotBeNull();
            GetProp(d2!, "TotalItems")!.Should().Be(3);
            GetProp(d2!, "ItemIndex")!.Should().Be(3);
        }

        //[Fact]
        //public async Task AcceptOrderDetailAsync_Sets_IN_PROD_And_OrderStatus_By_MinProductionStatus_When_NoError()
        //{
        //    var (db, svc, od1, od2, orderId, _, _) = await SeedAsync();

        //    var updated = await svc.AcceptOrderDetailAsync(od2);

        //    updated.Should().NotBeNull();
        //    updated!.ProductionStatus.Should().Be(ProductionStatus.IN_PROD);

        //    var order = await db.Orders.Include(o => o.OrderDetails).FirstAsync(o => o.OrderId == orderId);
        //    order.StatusOrder.Should().Be(2);
        //}

        [Fact]
        public async Task RejectOrderDetailAsync_Sets_QC_FAIL_Writes_Log_Sends_Notifications_And_OrderStatus_Is_QC_FAIL_Map()
        {
            var (db, svc, od1, _, orderId, um, notif) = await SeedAsync();

            var staffUsers = new List<AppUser>
            {
                new AppUser { Id = "staff-1", UserName = "staff1" },
                new AppUser { Id = "staff-2", UserName = "staff2" }
            };
            um.Setup(x => x.GetUsersInRoleAsync("Staff")).ReturnsAsync(staffUsers);

            var request = new QcRejectRequestDto { Reason = "Misaligned print area" };
            var qcUserId = "qc-user-01";

            var updated = await svc.RejectOrderDetailAsync(od1, request, qcUserId);

            updated.Should().NotBeNull();
            updated!.ProductionStatus.Should().Be(ProductionStatus.QC_FAIL);

            var logs = await db.OrderDetailLogs
                .Where(l => l.OrderDetailId == od1)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            logs.Should().NotBeEmpty();
            logs.First().ActorUserId.Should().Be(qcUserId);
            logs.First().Reason.Should().Be("Misaligned print area");
            logs.First().EventType.Should().Be("QC_REJECTED");

            var order = await db.Orders.Include(o => o.OrderDetails).FirstAsync(o => o.OrderId == orderId);
            order.StatusOrder.Should().Be(12);

            notif.Verify(n => n.CreateAndSendNotificationAsync(
                    "staff-1",
                    It.Is<string>(m => m.Contains("ORD100") && m.Contains("Misaligned print area")),
                    "/staff/needs-production"),
                Times.Once);

            notif.Verify(n => n.CreateAndSendNotificationAsync(
                    "staff-2",
                    It.Is<string>(m => m.Contains("ORD100") && m.Contains("Misaligned print area")),
                    "/staff/needs-production"),
                Times.Once);
        }

        [Fact]
        public async Task AcceptOrderDetailAsync_Returns_Null_When_NotFound()
        {
            var (db, svc, _, _, _, _, _) = await SeedAsync();

            var result = await svc.AcceptOrderDetailAsync(99999);
            result.Should().BeNull();
        }

        [Fact]
        public async Task RejectOrderDetailAsync_Returns_Null_When_NotFound()
        {
            var (db, svc, _, _, _, _, _) = await SeedAsync();

            var dto = new QcRejectRequestDto { Reason = "Invalid artwork" };
            var result = await svc.RejectOrderDetailAsync(99999, dto, "qc-123");
            result.Should().BeNull();
        }
    }
}
