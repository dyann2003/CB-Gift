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

        private static async Task<(CBGiftDbContext db, OrderDetailService svc, int od1, int od2, int orderId,
            Mock<UserManager<AppUser>> um, Mock<INotificationService> notif)> SeedAsync()
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

            // IMPORTANT: Match test case sheet -> valid orderDetailId = 1
            var odA = new OrderDetail
            {
                OrderDetailId = 1,
                OrderId = 100,
                Order = order,
                ProductVariantId = 10,
                ProductVariant = variant,
                Quantity = 2,
                ProductionStatus = ProductionStatus.CREATED
            };

            var odB = new OrderDetail
            {
                OrderDetailId = 2,
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

        // =========================
        // 1) getOrderDetailById (UTCD01-03)
        // =========================
        [Theory]
        [InlineData("UTCD01", 1, true, null, null)]
        [InlineData("UTCD02", 0, false, typeof(ArgumentException), "invalid")]
        // TODO: Replace typeof(KeyNotFoundException) by your project's NotFoundException type if you have it.
        [InlineData("UTCD03", 9999, false, typeof(KeyNotFoundException), "not found")]
        public async Task GetOrderDetailByIdAsync_Should_Follow_Testcase(
            string tcId,
            int orderDetailId,
            bool expectSuccess,
            Type? expectExceptionType,
            string? messageKeyword)
        {
            var (_, svc, _, _, _, _, _) = await SeedAsync();

            Func<Task> act = async () => { _ = await svc.GetOrderDetailByIdAsync(orderDetailId); };

            if (expectSuccess)
            {
                var result = await svc.GetOrderDetailByIdAsync(orderDetailId);
                result.Should().NotBeNull($"[{tcId}] should return data");

                // Optional: keep your existing deep assertions for success case
                var pv = GetProp(result!, "ProductVariant");
                pv.Should().NotBeNull();
                var prod = GetProp(pv!, "Product");
                prod.Should().NotBeNull();
                GetProp(prod!, "ProductName")!.Should().Be("Mug");
            }
            else
            {
                var ex = await act.Should().ThrowAsync<Exception>($"[{tcId}] should throw exception");
                ex.Which.Should().BeOfType(expectExceptionType!, $"[{tcId}] expected {expectExceptionType!.Name}");

                if (!string.IsNullOrWhiteSpace(messageKeyword))
                    ex.Which.Message.Should().Contain(messageKeyword!, $"[{tcId}] message should contain keyword");
            }
        }

        // =========================
        // 2) acceptOrderDetail (UTCD01-03)
        // =========================
        [Theory]
        [InlineData("UTCD01", 1, true, null, null)]
        [InlineData("UTCD02", 0, false, typeof(ArgumentException), "invalid")]
        // TODO: Replace typeof(KeyNotFoundException) by your project's NotFoundException type if you have it.
        [InlineData("UTCD03", 9999, false, typeof(KeyNotFoundException), "not found")]
        public async Task AcceptOrderDetailAsync_Should_Follow_Testcase(
            string tcId,
            int orderDetailId,
            bool expectSuccess,
            Type? expectExceptionType,
            string? messageKeyword)
        {
            var (db, svc, _, _, orderId, _, _) = await SeedAsync();

            Func<Task> act = async () => { _ = await svc.AcceptOrderDetailAsync(orderDetailId); };

            if (expectSuccess)
            {
                var updated = await svc.AcceptOrderDetailAsync(orderDetailId);
                updated.Should().NotBeNull($"[{tcId}] should succeed");
                updated!.ProductionStatus.Should().Be(ProductionStatus.IN_PROD);

                // If your service updates Order.StatusOrder based on min production status:
                var order = await db.Orders.Include(o => o.OrderDetails).FirstAsync(o => o.OrderId == orderId);
                order.Should().NotBeNull();
                // NOTE: Keep/adjust expected status mapping according to your business rule.
                // order.StatusOrder.Should().Be(expectedStatusId);
            }
            else
            {
                var ex = await act.Should().ThrowAsync<Exception>($"[{tcId}] should throw exception");
                ex.Which.Should().BeOfType(expectExceptionType!, $"[{tcId}] expected {expectExceptionType!.Name}");

                if (!string.IsNullOrWhiteSpace(messageKeyword))
                    ex.Which.Message.Should().Contain(messageKeyword!, $"[{tcId}] message should contain keyword");
            }
        }

        // =========================
        // 3) rejectOrderDetail (UTCD01-03)
        // =========================
        [Theory]
        [InlineData("UTCD02", 0, typeof(ArgumentException), "invalid")]
        // TODO: Replace typeof(KeyNotFoundException) by your project's NotFoundException type if you have it.
        [InlineData("UTCD03", 9999, typeof(KeyNotFoundException), "not found")]
        public async Task RejectOrderDetailAsync_Should_Throw_For_Invalid_And_NotFound(
            string tcId,
            int orderDetailId,
            Type expectExceptionType,
            string messageKeyword)
        {
            var (_, svc, _, _, _, _, _) = await SeedAsync();

            var dto = new QcRejectRequestDto { Reason = "Misaligned print area" };

            Func<Task> act = async () => { _ = await svc.RejectOrderDetailAsync(orderDetailId, dto, "qc-user-01"); };

            var ex = await act.Should().ThrowAsync<Exception>($"[{tcId}] should throw exception");
            ex.Which.Should().BeOfType(expectExceptionType, $"[{tcId}] expected {expectExceptionType.Name}");
            ex.Which.Message.Should().Contain(messageKeyword, $"[{tcId}] message should contain keyword");
        }

        [Fact]
        public async Task RejectOrderDetailAsync_UTCD01_Sets_QC_FAIL_Writes_Log_Sends_Notifications_And_OrderStatus_Is_QC_FAIL_Map()
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
    }
}
