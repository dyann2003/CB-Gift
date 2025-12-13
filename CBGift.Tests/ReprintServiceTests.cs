using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Hubs;
using CB_Gift.Models;
using CB_Gift.Models.Enums;
using CB_Gift.Services;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CB_Gift.Tests.Services
{
    public class ReprintServiceTests
    {
        private static CBGiftDbContext NewDb()
        {
            var opt = new DbContextOptionsBuilder<CBGiftDbContext>()
                .UseInMemoryDatabase($"reprint-{Guid.NewGuid()}")
                .Options;

            return new CBGiftDbContext(opt);
        }

        private static Mock<UserManager<AppUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<AppUser>>();
            return new Mock<UserManager<AppUser>>(
                store.Object,
                null, null, null, null, null, null, null, null
            );
        }

        private static Mock<IHubContext<NotificationHub>> MockHubContext(out Mock<IClientProxy> clientProxy)
        {
            clientProxy = new Mock<IClientProxy>();

            var hubClients = new Mock<IHubClients>();
            hubClients.Setup(c => c.Group(It.IsAny<string>()))
                      .Returns(clientProxy.Object);
            hubClients.Setup(c => c.User(It.IsAny<string>()))
                      .Returns(clientProxy.Object);

            var hub = new Mock<IHubContext<NotificationHub>>();
            hub.Setup(h => h.Clients).Returns(hubClients.Object);

            return hub;
        }

        private static ReprintService BuildService(
            CBGiftDbContext db,
            Mock<IOrderService>? orderSvc = null,
            Mock<INotificationService>? notiSvc = null,
            Mock<IHubContext<NotificationHub>>? hub = null,
            Mock<UserManager<AppUser>>? userManager = null)
        {
            orderSvc ??= new Mock<IOrderService>();
            notiSvc ??= new Mock<INotificationService>();
            userManager ??= MockUserManager();
            hub ??= MockHubContext(out _);

            var mapper = new Mock<IMapper>();
            var logger = new Mock<ILogger<ReprintService>>();

            return new ReprintService(
                db,
                orderSvc.Object,
                mapper.Object,
                logger.Object,
                notiSvc.Object,
                hub.Object,
                userManager.Object
            );
        }

        // -------------------------
        // Helpers: seed minimum data
        // -------------------------

        private static Order SeedOrder(CBGiftDbContext db, int orderId, string sellerId, string orderCode, string paymentStatus = "Paid")
        {
            var customer = new EndCustomer
            {
                CustId = orderId,
                Name = "Cust",
                Phone = "0900",
                Email = "c@x.com",
                Address = "A",
                Address1 = "A1",
                Zipcode = "10000",
                ShipState = "State",
                ShipCity = "City",
                ShipCountry = "VN"
            };

            var order = new Order
            {
                OrderId = orderId,
                SellerUserId = sellerId,
                OrderCode = orderCode,
                PaymentStatus = paymentStatus,
                StatusOrder = 8,
                ToDistrictId = 1,
                ToProvinceId = 1,
                ToWardCode = "W",
                ActiveTts = true,
                Tracking = "T",
                TotalCost = 1000,
                EndCustomer = customer
            };

            db.EndCustomers.Add(customer);
            db.Orders.Add(order);
            return order;
        }

        private static OrderDetail SeedOrderDetail(CBGiftDbContext db, int odId, int orderId, int productVariantId, int qty = 1)
        {
            var product = new Product { ProductId = productVariantId, ProductName = $"P{productVariantId}" };
            var pv = new ProductVariant { ProductVariantId = productVariantId, ProductId = product.ProductId, Product = product, Sku = $"SKU-{productVariantId}" };

            db.Products.Add(product);
            db.ProductVariants.Add(pv);

            var od = new OrderDetail
            {
                OrderDetailId = odId,
                OrderId = orderId,
                ProductVariantId = productVariantId,
                ProductVariant = pv,
                Quantity = qty,
                Price = 100,
                NeedDesign = false,
                LinkImg = "img",
                LinkFileDesign = "design",
                LinkThanksCard = "thanks",
                Accessory = "acc",
                Note = "note",
                ProductionStatus = ProductionStatus.QC_DONE
            };

            db.OrderDetails.Add(od);
            return od;
        }

        private static Reprint SeedReprint(CBGiftDbContext db, int reprintId, int originalOrderDetailId, string requestedBy, string status = "Pending", string reason = "R", string proofUrl = "proof")
        {
            var r = new Reprint
            {
                Id = reprintId,
                OriginalOrderDetailId = originalOrderDetailId,
                RequestedBy = requestedBy,
                Status = status,
                Reason = reason,
                ProofUrl = proofUrl,
                RequestDate = DateTime.Now,
                Processed = false
            };
            db.Reprints.Add(r);
            return r;
        }

        private static void SeedInvoicePaidRecently(CBGiftDbContext db, int orderId, string invoiceStatus = "Paid", DateTime? lastPaymentUtc = null)
        {
            lastPaymentUtc ??= DateTime.UtcNow.AddDays(-1);

            var inv = new Invoice
            {
                InvoiceId = orderId,
                InvoiceNumber = $"INV-{orderId}",
                Status = invoiceStatus,
                TotalAmount = 1000,
                AmountPaid = invoiceStatus == "Paid" ? 1000 : 0,
                Payments = new List<Payment>()
            };

            inv.Payments.Add(new Payment
            {
                PaymentId = orderId,
                Status = "Paid",
                PaymentDate = lastPaymentUtc.Value
            });

            var item = new InvoiceItem
            {
                InvoiceItemId = orderId,
                OrderId = orderId,
                InvoiceId = inv.InvoiceId,
                Invoice = inv
            };

            db.Invoices.Add(inv);
            db.InvoiceItems.Add(item);
        }

        // =========================
        // ApproveReprintAsync Tests
        // =========================

        [Fact]
        public async Task ApproveReprintAsync_Throws_When_List_Empty()
        {
            using var db = NewDb();
            var svc = BuildService(db);

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                svc.ApproveReprintAsync(new ReprintManagerDto { OriginalOrderDetailIds = new List<int>() }, "mgr"));

            Assert.Contains("Danh sách sản phẩm trống", ex.Message);
        }

        [Fact]
        public async Task ApproveReprintAsync_Throws_When_Different_Orders()
        {
            using var db = NewDb();

            SeedOrder(db, 1, "seller", "OC-1");
            SeedOrder(db, 2, "seller", "OC-2");
            SeedOrderDetail(db, 11, 1, 101);
            SeedOrderDetail(db, 22, 2, 202);

            SeedReprint(db, 1, 11, "seller");
            SeedReprint(db, 2, 22, "seller");

            await db.SaveChangesAsync();

            var svc = BuildService(db);

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                svc.ApproveReprintAsync(new ReprintManagerDto { OriginalOrderDetailIds = new List<int> { 11, 22 } }, "mgr"));

            Assert.Contains("khác nhau", ex.Message);
        }

        [Fact]
        public async Task ApproveReprintAsync_Works_And_Creates_New_OrderCode_RE()
        {
            using var db = NewDb();

            var sellerId = "seller-1";
            var original = SeedOrder(db, 1, sellerId, "OC-1");
            SeedOrderDetail(db, 11, 1, 101);
            SeedOrderDetail(db, 12, 1, 102);

            SeedReprint(db, 1, 11, sellerId, status: "Pending", reason: "Broken", proofUrl: "u");
            SeedReprint(db, 2, 12, sellerId, status: "Pending", reason: "Broken", proofUrl: "u");

            await db.SaveChangesAsync();

            var orderSvc = new Mock<IOrderService>();
            orderSvc
       .Setup(s => s.MakeOrder(It.IsAny<MakeOrderDto>(), sellerId))
       .ReturnsAsync(new MakeOrderResponse
       {
           OrderId = 999,
           
       });

            // Khi Approve gọi lại context.Orders.Include(...).FirstOrDefaultAsync(...)
            // nên seed order mới để query ra được (hoặc bạn có thể mock OrderService tạo trong DB thật)
            db.Orders.Add(new Order
            {
                OrderId = 999,
                OrderCode = "OC-1_RE",
                SellerUserId = sellerId,
                PaymentStatus = "Paid",
                ProductionStatus = "Reprint",
                StatusOrder = 8,
                OrderDetails = new List<OrderDetail>
                {
                    new OrderDetail { OrderDetailId = 9991, OrderId = 999, ProductVariantId = 101, Quantity = 1, Price=0, ProductionStatus = ProductionStatus.READY_PROD },
                    new OrderDetail { OrderDetailId = 9992, OrderId = 999, ProductVariantId = 102, Quantity = 1, Price=0, ProductionStatus = ProductionStatus.READY_PROD }
                }
            });

            await db.SaveChangesAsync();

            var notiSvc = new Mock<INotificationService>();
            var hub = MockHubContext(out var clientProxy);

            var svc = BuildService(db, orderSvc, notiSvc, hub);

            await svc.ApproveReprintAsync(new ReprintManagerDto
            {
                OriginalOrderDetailIds = new List<int> { 11, 12 }
            }, managerId: "mgr-1");

            // Assert: reprints updated
            var reprints = await db.Reprints.OrderBy(r => r.Id).ToListAsync();
            Assert.All(reprints, r =>
            {
                Assert.True(r.Processed);
                Assert.Equal("Approved", r.Status);
                Assert.Equal("mgr-1", r.ManagerAcceptedBy);
            });

            // Assert: original order status updated
            var reloadedOriginal = await db.Orders.FindAsync(1);
            Assert.NotNull(reloadedOriginal);
            Assert.Equal(15, reloadedOriginal!.StatusOrder);

            // Assert: original details status updated
            var ods = await db.OrderDetails.Where(x => x.OrderId == 1).ToListAsync();
            Assert.All(ods, od => Assert.Equal(ProductionStatus.PROD_REWORK, od.ProductionStatus));

            // Assert: notification invoked
            notiSvc.Verify(n => n.CreateAndSendNotificationAsync(
                sellerId,
                It.Is<string>(s => s.Contains("DUYỆT")),
                It.Is<string>(url => url.Contains("/seller/order-view/999"))
            ), Times.Once);

            // Assert: hub send invoked (seller)
            clientProxy.Verify(c => c.SendCoreAsync(
                "ReprintStatusChanged",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()
            ), Times.AtLeastOnce);
        }

        // ========================
        // RejectReprintAsync Tests
        // ========================

        [Fact]
        public async Task RejectReprintAsync_Throws_When_RejectReason_Empty()
        {
            using var db = NewDb();
            var svc = BuildService(db);

            var ex = await Assert.ThrowsAsync<Exception>(() =>
                svc.RejectReprintAsync(
                    new ReprintManagerDto { OriginalOrderDetailIds = new List<int> { 1 }, RejectReason = "" },
                    "mgr"));

            Assert.Contains("Lý do từ chối", ex.Message);
        }

        [Fact]
        public async Task RejectReprintAsync_Works()
        {
            using var db = NewDb();

            var sellerId = "seller-1";
            SeedOrder(db, 1, sellerId, "OC-1");
            SeedOrderDetail(db, 11, 1, 101);

            SeedReprint(db, 1, 11, sellerId, status: "Pending", reason: "Bad", proofUrl: "u");
            await db.SaveChangesAsync();

            var notiSvc = new Mock<INotificationService>();
            var hub = MockHubContext(out var clientProxy);

            var svc = BuildService(db, notiSvc: notiSvc, hub: hub);

            await svc.RejectReprintAsync(
                new ReprintManagerDto { OriginalOrderDetailIds = new List<int> { 11 }, RejectReason = "No proof" },
                managerId: "mgr-1"
            );

            var r = await db.Reprints.FirstAsync();
            Assert.True(r.Processed);
            Assert.Equal("Rejected", r.Status);
            Assert.Equal("mgr-1", r.ManagerAcceptedBy);
            Assert.Equal("No proof", r.StaffRejectionReason);

            var od = await db.OrderDetails.FirstAsync();
            Assert.Equal(ProductionStatus.QC_DONE, od.ProductionStatus);

            var order = await db.Orders.FirstAsync();
            Assert.Equal(14, order.StatusOrder);

            notiSvc.Verify(n => n.CreateAndSendNotificationAsync(
                sellerId,
                It.Is<string>(s => s.Contains("TỪ CHỐI")),
                It.Is<string>(url => url.Contains("/seller/order-view/1"))
            ), Times.Once);

            clientProxy.Verify(c => c.SendCoreAsync(
                "ReprintStatusChanged",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()
            ), Times.AtLeastOnce);
        }

        // =========================
        // RequestReprintAsync Tests
        // =========================

        [Fact]
        public async Task RequestReprintAsync_Throws_When_Order_Unpaid()
        {
            using var db = NewDb();

            var sellerId = "seller-1";
            SeedOrder(db, 1, sellerId, "OC-1", paymentStatus: "Unpaid");
            SeedOrderDetail(db, 11, 1, 101);
            await db.SaveChangesAsync();

            var svc = BuildService(db);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.RequestReprintAsync(new SellerReprintRequestDto
                {
                    OrderId = 1,
                    Reason = "R",
                    ProofUrl = "u",
                    SelectedItems = new List<ReprintItemRequestDto>
                    {
                        new ReprintItemRequestDto { OriginalOrderDetailId = 11 }
                    }
                }, sellerId));

            Assert.Contains("paid", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

     
       
        // ===========================================
        // GetReviewReprintRequestsPaginatedAsync Tests
        // ===========================================

        [Fact]
        public async Task GetReviewReprintRequestsPaginatedAsync_Groups_By_Order_Reason_Proof_Status()
        {
            using var db = NewDb();

            var sellerId = "seller-1";
            SeedOrder(db, 1, sellerId, "OC-1");
            SeedOrderDetail(db, 11, 1, 101);
            SeedOrderDetail(db, 12, 1, 102);

            // Same group key -> should group to 1
            SeedReprint(db, 1, 11, sellerId, status: "Pending", reason: "R1", proofUrl: "U1");
            SeedReprint(db, 2, 12, sellerId, status: "Pending", reason: "R1", proofUrl: "U1");

            // Different group -> another row
            SeedOrder(db, 2, sellerId, "OC-2");
            SeedOrderDetail(db, 21, 2, 201);
            SeedReprint(db, 3, 21, sellerId, status: "Pending", reason: "R2", proofUrl: "U2");

            await db.SaveChangesAsync();

            var svc = BuildService(db);

            var result = await svc.GetReviewReprintRequestsPaginatedAsync(
                staffId: "staff",
                searchTerm: null,
                filterType: null,
                sellerIdFilter: null,
                statusFilter: "pending",
                page: 1,
                pageSize: 10
            );

            // Expect 2 groups
            Assert.Equal(2, result.Total);
            Assert.Equal(2, result.Items.Count);

            // Group with 2 items should be ORDER-WIDE
            var group2Items = result.Items.FirstOrDefault(i => i.CountOfItems == 2);
            Assert.NotNull(group2Items);
            Assert.Equal("ORDER-WIDE", group2Items!.TargetLevel);
        }

        // ==========================
        // GetReprintDetailsAsync Tests
        // ==========================

        [Fact]
        public async Task GetReprintDetailsAsync_Returns_Null_When_NotFound()
        {
            using var db = NewDb();
            var svc = BuildService(db);

            var res = await svc.GetReprintDetailsAsync(999);
            Assert.Null(res);
        }

        [Fact]
        public async Task GetReprintDetailsAsync_Returns_All_Items_In_Same_Group()
        {
            using var db = NewDb();

            var sellerId = "seller-1";
            SeedOrder(db, 1, sellerId, "OC-1");
            SeedOrderDetail(db, 11, 1, 101);
            SeedOrderDetail(db, 12, 1, 102);

            // Same group key (OrderId + Reason + ProofUrl + Status)
            SeedReprint(db, 1, 11, sellerId, status: "Pending", reason: "R1", proofUrl: "U1");
            SeedReprint(db, 2, 12, sellerId, status: "Pending", reason: "R1", proofUrl: "U1");

            // Different group -> should not be included
            SeedReprint(db, 3, 12, sellerId, status: "Approved", reason: "R1", proofUrl: "U1");

            await db.SaveChangesAsync();

            var svc = BuildService(db);

            var details = await svc.GetReprintDetailsAsync(1);

            Assert.NotNull(details);
            Assert.Equal(1, details!.Id);
            Assert.Equal(1, details.OrderId);
            Assert.Equal("OC-1", details.OrderCode);
            Assert.Equal("Pending", details.Status);
            Assert.Equal("R1", details.Reason);
            Assert.Equal("U1", details.ProofUrl);

            // Must include 2 items in same group
            Assert.Equal(2, details.RequestedItems.Count);
            Assert.Contains(details.RequestedItems, x => x.OrderDetailId == 11);
            Assert.Contains(details.RequestedItems, x => x.OrderDetailId == 12);
        }
    }
}
