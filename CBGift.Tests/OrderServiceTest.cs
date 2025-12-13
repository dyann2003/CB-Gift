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
using CB_Gift.Orders.Import;
using CB_Gift.Services;
using CB_Gift.Services.IService;
using CB_Gift.Tests.Utils;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CB_Gift.Tests.Services
{
    public class OrderServiceTests
    {
        private readonly CBGiftDbContext _db;

        // Mapper: mock + ConfigurationProvider cho ProjectTo
        private readonly Mock<IMapper> _mapperMock;
        private readonly MapperConfiguration _mapperConfig;

        private readonly ILogger<OrderService> _logger;

        // Notification + SignalR
        private readonly Mock<INotificationService> _notifyMock;
        private readonly Mock<IHubContext<NotificationHub>> _hubContextMock;
        private readonly Mock<IHubClients> _hubClientsMock;
        private readonly Mock<IClientProxy> _clientProxyMock;

        // NEW deps
        private readonly Mock<IShippingService> _shippingMock;
        private readonly Mock<OrderFactory> _orderFactoryMock;
        private readonly Mock<IValidator<OrderImportRowDto>> _validatorMock;
        private readonly Mock<ReferenceDataCache> _cacheMock;

        private readonly OrderService _svc;

        public OrderServiceTests()
        {
            _db = InMemoryDbFactory.CreateContext();

            _logger = Mock.Of<ILogger<OrderService>>();

            // ---------- AutoMapper config tối thiểu cho ProjectTo ----------
            _mapperConfig = BuildMapperConfig();

            _mapperMock = new Mock<IMapper>(MockBehavior.Loose);
            _mapperMock.SetupGet(m => m.ConfigurationProvider).Returns(_mapperConfig);

            // ---------- Notification ----------
            _notifyMock = new Mock<INotificationService>(MockBehavior.Strict);
            _notifyMock
                .Setup(n => n.CreateAndSendNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // ---------- SignalR ----------
            _clientProxyMock = new Mock<IClientProxy>(MockBehavior.Strict);
            _clientProxyMock
                .Setup(cp => cp.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _hubClientsMock = new Mock<IHubClients>(MockBehavior.Strict);
            _hubClientsMock.Setup(h => h.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);

            _hubContextMock = new Mock<IHubContext<NotificationHub>>(MockBehavior.Strict);
            _hubContextMock.SetupGet(h => h.Clients).Returns(_hubClientsMock.Object);

            // ---------- Shipping ----------
            _shippingMock = new Mock<IShippingService>(MockBehavior.Strict);

            // ---------- Import-related ----------
            // Nếu OrderFactory có ctor khác null thì phải truyền đúng ctor args thật
            _orderFactoryMock = new Mock<OrderFactory>(MockBehavior.Loose, /* ctor args if any */ null);
            _validatorMock = new Mock<IValidator<OrderImportRowDto>>(MockBehavior.Strict);

            // IMPORTANT:
            // ReferenceDataCache phải có method virtual (LoadAsync, LoadExistingOrderCodesAsync)
            // thì Setup dưới đây mới không bị NotSupportedException.
            _cacheMock = new Mock<ReferenceDataCache>(MockBehavior.Loose);

            // Default setups cho cache load (nếu test gọi import/validate)
            _cacheMock.Setup(c => c.LoadAsync()).Returns(Task.CompletedTask);
            _cacheMock.Setup(c => c.LoadExistingOrderCodesAsync(It.IsAny<List<string>>()))
                      .Returns(Task.CompletedTask);

            _svc = new OrderService(
                _db,
                _mapperMock.Object,
                _logger,
                _notifyMock.Object,
                _hubContextMock.Object,
                _orderFactoryMock.Object,
                _validatorMock.Object,
                _cacheMock.Object,
                _shippingMock.Object
            );

            Seed(_db);
        }

        private static MapperConfiguration BuildMapperConfig()
        {
            return new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Order, OrderWithDetailsDto>(MemberList.None)
                    .ForMember(d => d.OrderId, o => o.MapFrom(s => s.OrderId))
                    .ForMember(d => d.OrderCode, o => o.MapFrom(s => s.OrderCode))
                    .ForMember(d => d.OrderDate, o => o.MapFrom(s => s.OrderDate))
                    .ForMember(d => d.TotalCost, o => o.MapFrom(s => s.TotalCost))
                    .ForMember(d => d.SellerId, o => o.MapFrom(s => s.SellerUserId))
                    .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.EndCustomer != null ? s.EndCustomer.Name : null));
            });
        }

        private static void Seed(CBGiftDbContext db)
        {
            // Status
            var stDraft = new OrderStatus { StatusId = 1, Code = "DRAFT", NameVi = "Nháp" };
            var stCheckDesign = new OrderStatus { StatusId = 5, Code = "CHECKDESIGN", NameVi = "Duyệt thiết kế" };
            var stShipping = new OrderStatus { StatusId = 13, Code = "SHIPPING", NameVi = "Đang giao" };

            var p = new Product { ProductId = 1, ProductName = "Mug", ProductCode = "P001", CategoryId = 1, Status = 1, Describe = "desc" };

            // IMPORTANT: ProductVariant scaffold của bạn có nhiều string NOT NULL -> set dummy để tránh null.
            var v1 = new ProductVariant
            {
                ProductVariantId = 10,
                ProductId = 1,
                Product = p,
                Sku = "MUG-RED",
                ThicknessMm = "2",
                SizeInch = "12",
                Layer = "1",
                CustomShape = "false",
                BaseCost = 5m,
                ShipCost = 2m,
                ExtraShipping = 1m,
                WeightGram = 100m,
                LengthCm = 10m,
                WidthCm = 10m,
                HeightCm = 10m
            };

            var v2 = new ProductVariant
            {
                ProductVariantId = 11,
                ProductId = 1,
                Product = p,
                Sku = "MUG-BLUE",
                ThicknessMm = "2",
                SizeInch = "12",
                Layer = "1",
                CustomShape = "false",
                BaseCost = 7m,
                ShipCost = 3m,
                ExtraShipping = 2m,
                WeightGram = 120m,
                LengthCm = 10m,
                WidthCm = 10m,
                HeightCm = 10m
            };

            var c = new EndCustomer { CustId = 1, Name = "John", Email = "john@x.com", Phone = "090", Address = "Addr" };

            var o1 = new Order
            {
                OrderId = 100,
                OrderCode = "ORD100",
                SellerUserId = "sellerA",
                EndCustomerId = 1,
                EndCustomer = c,
                OrderDate = DateTime.UtcNow.AddDays(-1),
                StatusOrder = 1,
                StatusOrderNavigation = stDraft,
                TotalCost = 0,
                ToDistrictId = 1,
                ToWardCode = "W1"
            };

            var o2 = new Order
            {
                OrderId = 101,
                OrderCode = "ORD101",
                SellerUserId = "sellerB",
                EndCustomerId = 1,
                EndCustomer = c,
                OrderDate = DateTime.UtcNow,
                StatusOrder = 1,
                StatusOrderNavigation = stDraft,
                TotalCost = 0,
                ToDistrictId = 1,
                ToWardCode = "W1"
            };

            var od1 = new OrderDetail
            {
                OrderDetailId = 1000,
                OrderId = 100,
                Order = o1,
                ProductVariantId = 10,
                ProductVariant = v1,
                Quantity = 1,
                NeedDesign = true,
                ProductionStatus = ProductionStatus.CHECK_DESIGN,
                AssignedDesignerUserId = "designerX"
            };

            var od2 = new OrderDetail
            {
                OrderDetailId = 1001,
                OrderId = 100,
                Order = o1,
                ProductVariantId = 11,
                ProductVariant = v2,
                Quantity = 2,
                NeedDesign = true,
                ProductionStatus = ProductionStatus.CHECK_DESIGN,
                AssignedDesignerUserId = "designerX"
            };

            var od3 = new OrderDetail
            {
                OrderDetailId = 1002,
                OrderId = 101,
                Order = o2,
                ProductVariantId = 10,
                ProductVariant = v1,
                Quantity = 1,
                NeedDesign = false,
                ProductionStatus = ProductionStatus.DRAFT
            };

            db.OrderStatuses.AddRange(stDraft, stCheckDesign, stShipping);
            db.Products.Add(p);
            db.ProductVariants.AddRange(v1, v2);
            db.EndCustomers.Add(c);
            db.Orders.AddRange(o1, o2);
            db.OrderDetails.AddRange(od1, od2, od3);
            db.SaveChanges();
        }

        // =========================================================
        // CreateCustomerAsync
        // =========================================================
        [Fact]
        public async Task CreateCustomerAsync_Creates_And_Returns()
        {
            var req = new EndCustomerCreateRequest
            {
                Name = "Jane",
                Email = "jane@x.com",
                Phone = "0123",
                Address = "A",
                Address1 = "A1",
                ZipCode = "70000",
                ShipState = "S",
                ShipCity = "C",
                ShipCountry = "VN"
            };

            var res = await _svc.CreateCustomerAsync(req);

            res.Should().NotBeNull();
            res.CustId.Should().BeGreaterThan(0);
            res.Name.Should().Be("Jane");
        }

        // =========================================================
        // CreateOrderAsync (ActiveTTS true/false)
        // =========================================================
        [Fact]
        public async Task CreateOrderAsync_ActiveTTS_Adds_Surcharge_1()
        {
            var req = new OrderCreateRequest { ActiveTTS = true };

            _mapperMock.Setup(m => m.Map<Order>(It.IsAny<OrderCreateRequest>()))
                .Returns((OrderCreateRequest r) => new Order { ActiveTts = r.ActiveTTS, TotalCost = 0 });

            var id = await _svc.CreateOrderAsync(req, "sellerZ");

            var inDb = await _db.Orders.FindAsync(id);
            inDb.Should().NotBeNull();
            inDb!.SellerUserId.Should().Be("sellerZ");
            inDb.StatusOrder.Should().Be(1);
            inDb.ProductionStatus.Should().Be("CREATED");
            inDb.TotalCost.Should().Be(1);
        }

        [Fact]
        public async Task CreateOrderAsync_NoTTS_No_Surcharge()
        {
            var req = new OrderCreateRequest { ActiveTTS = false };

            _mapperMock.Setup(m => m.Map<Order>(It.IsAny<OrderCreateRequest>()))
                .Returns((OrderCreateRequest r) => new Order { ActiveTts = r.ActiveTTS, TotalCost = 0 });

            var id = await _svc.CreateOrderAsync(req, "sellerZ");

            var inDb = await _db.Orders.FindAsync(id);
            inDb.Should().NotBeNull();
            inDb!.TotalCost.Should().Be(0);
        }

        // =========================================================
        // AddOrderDetailAsync (owner / variant exists / quantity / recalc)
        // =========================================================
        [Fact]
        public async Task AddOrderDetailAsync_Throws_When_NotOwner()
        {
            var req = new OrderDetailCreateRequest { ProductVariantID = 10, Quantity = 1 };

            _mapperMock.Setup(m => m.Map<OrderDetail>(It.IsAny<OrderDetailCreateRequest>()))
                .Returns(new OrderDetail { ProductVariantId = 10, Quantity = 1 });

            var act = async () => await _svc.AddOrderDetailAsync(101, req, "sellerA");
            await act.Should().ThrowAsync<Exception>().WithMessage("*not yours*");
        }

        [Fact]
        public async Task AddOrderDetailAsync_Throws_When_Variant_NotFound()
        {
            var req = new OrderDetailCreateRequest { ProductVariantID = 999, Quantity = 1 };
            _mapperMock.Setup(m => m.Map<OrderDetail>(It.IsAny<OrderDetailCreateRequest>()))
                .Returns(new OrderDetail());

            var act = async () => await _svc.AddOrderDetailAsync(100, req, "sellerA");
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*does not exist*");
        }

        [Fact]
        public async Task AddOrderDetailAsync_Throws_When_Quantity_Invalid()
        {
            var req = new OrderDetailCreateRequest { ProductVariantID = 10, Quantity = 0 };
            _mapperMock.Setup(m => m.Map<OrderDetail>(It.IsAny<OrderDetailCreateRequest>()))
                .Returns(new OrderDetail());

            var act = async () => await _svc.AddOrderDetailAsync(100, req, "sellerA");
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*greater than zero*");
        }

        [Fact]
        public async Task AddOrderDetailAsync_Adds_And_Recalculates_TotalCost()
        {
            // Order 100 đang có: v10 qty1, v11 qty2.
            // Add thêm v10 qty3 => tổng theo công thức của bạn = 47
            var req = new OrderDetailCreateRequest { ProductVariantID = 10, Quantity = 3 };

            _mapperMock.Setup(m => m.Map<OrderDetail>(It.IsAny<OrderDetailCreateRequest>()))
                .Returns((OrderDetailCreateRequest r) => new OrderDetail
                {
                    ProductVariantId = r.ProductVariantID,
                    Quantity = r.Quantity,
                    NeedDesign = false
                });

            await _svc.AddOrderDetailAsync(100, req, "sellerA");

            var order = await _db.Orders.FindAsync(100);
            order.Should().NotBeNull();
            order!.TotalCost.Should().Be(47);
        }

        // =========================================================
        // DeleteOrderAsync (not found / status != 1 / success)
        // =========================================================
        [Fact]
        public async Task DeleteOrderAsync_ReturnsFalse_When_NotFound_Or_NotOwned()
        {
            (await _svc.DeleteOrderAsync(999, "sellerA")).Should().BeFalse();
            (await _svc.DeleteOrderAsync(101, "sellerA")).Should().BeFalse();
        }

        [Fact]
        public async Task DeleteOrderAsync_Throws_When_Status_Not_Draft()
        {
            var o = await _db.Orders.FindAsync(100);
            o!.StatusOrder = 2;
            await _db.SaveChangesAsync();

            var act = async () => await _svc.DeleteOrderAsync(100, "sellerA");
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Daft(Nháp)*");
        }

        [Fact]
        public async Task DeleteOrderAsync_Removes_Order_And_Details_When_Draft()
        {
            var ok = await _svc.DeleteOrderAsync(100, "sellerA");
            ok.Should().BeTrue();

            (await _db.Orders.FindAsync(100)).Should().BeNull();
            (await _db.OrderDetails.Where(d => d.OrderId == 100).ToListAsync()).Should().BeEmpty();
        }

        // =========================================================
        // SellerApproveOrderDesignAsync
        // =========================================================
        [Fact]
        public async Task SellerApproveOrderDesignAsync_ReturnsFalse_When_Order_NotFound()
        {
            var ok = await _svc.SellerApproveOrderDesignAsync(999, ProductionStatus.READY_PROD, "sellerA");
            ok.Should().BeFalse();
        }

        [Fact]
        public async Task SellerApproveOrderDesignAsync_Throws_When_WrongSeller()
        {
            var o = await _db.Orders.FindAsync(100);
            o!.StatusOrder = 5;
            await _db.SaveChangesAsync();

            var act = async () => await _svc.SellerApproveOrderDesignAsync(100, ProductionStatus.READY_PROD, "other");
            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*not authorized*");
        }

        [Fact]
        public async Task SellerApproveOrderDesignAsync_Throws_When_OrderStatus_Not_CheckDesign()
        {
            var o = await _db.Orders.FindAsync(100);
            o!.StatusOrder = 4;
            await _db.SaveChangesAsync();

            var act = async () => await _svc.SellerApproveOrderDesignAsync(100, ProductionStatus.READY_PROD, "sellerA");
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not in CHECK_DESIGN*");
        }

        [Fact]
        public async Task SellerApproveOrderDesignAsync_Throws_When_NotAllDetails_In_CheckDesign()
        {
            var o = await _db.Orders.Include(x => x.OrderDetails).FirstAsync(x => x.OrderId == 100);
            o.StatusOrder = 5;
            o.OrderDetails.First().ProductionStatus = ProductionStatus.DESIGN_REDO;
            await _db.SaveChangesAsync();

            var act = async () => await _svc.SellerApproveOrderDesignAsync(100, ProductionStatus.READY_PROD, "sellerA");
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Not all OrderDetails*");
        }

        [Fact]
        public async Task SellerApproveOrderDesignAsync_Throws_When_Action_Invalid()
        {
            var o = await _db.Orders.Include(x => x.OrderDetails).FirstAsync(x => x.OrderId == 101);
            o.StatusOrder = 5;
            foreach (var d in o.OrderDetails) d.ProductionStatus = ProductionStatus.CHECK_DESIGN;
            await _db.SaveChangesAsync();

            var act = async () => await _svc.SellerApproveOrderDesignAsync(101, ProductionStatus.DRAFT, "sellerB");
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task SellerApproveOrderDesignAsync_Success_Sends_Notification_And_SignalR()
        {
            var o = await _db.Orders.Include(x => x.OrderDetails).FirstAsync(x => x.OrderId == 100);
            o.StatusOrder = 5;
            foreach (var d in o.OrderDetails)
            {
                d.NeedDesign = true;
                d.ProductionStatus = ProductionStatus.CHECK_DESIGN;
                d.AssignedDesignerUserId = "designerX";
            }
            await _db.SaveChangesAsync();

            var ok = await _svc.SellerApproveOrderDesignAsync(100, ProductionStatus.READY_PROD, "sellerA");
            ok.Should().BeTrue();

            (await _db.Orders.FindAsync(100))!.StatusOrder.Should().Be(7);

            _notifyMock.Verify(n => n.CreateAndSendNotificationAsync(
                "designerX",
                It.Is<string>(msg => msg.Contains("ACCEPT", StringComparison.OrdinalIgnoreCase)),
                It.IsAny<string>()), Times.AtLeastOnce);

            _hubClientsMock.Verify(h => h.Group("order_100"), Times.AtLeastOnce);
            _clientProxyMock.Verify(cp => cp.SendCoreAsync(
                "OrderStatusChanged",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        // =========================================================
        // SellerApproveOrderDetailDesignAsync
        // =========================================================
        [Fact]
        public async Task SellerApproveOrderDetailDesignAsync_ReturnsFalse_When_Order_NotFound()
        {
            var ok = await _svc.SellerApproveOrderDetailDesignAsync(99999, ProductionStatus.READY_PROD, "sellerA", "x");
            ok.Should().BeFalse();
        }

        [Fact]
        public async Task SellerApproveOrderDetailDesignAsync_Throws_When_WrongSeller()
        {
            var act = async () => await _svc.SellerApproveOrderDetailDesignAsync(1000, ProductionStatus.READY_PROD, "wrong", "x");
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task SellerApproveOrderDetailDesignAsync_Throws_When_Not_In_CheckDesign()
        {
            var d = await _db.OrderDetails.Include(x => x.Order).FirstAsync(x => x.OrderDetailId == 1002); // order 101
            d.Order!.SellerUserId = "sellerB";
            d.ProductionStatus = ProductionStatus.DESIGN_REDO;
            await _db.SaveChangesAsync();

            var act = async () => await _svc.SellerApproveOrderDetailDesignAsync(1002, ProductionStatus.READY_PROD, "sellerB", "x");
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Must be CHECK_DESIGN*");
        }

        [Fact]
        public async Task SellerApproveOrderDetailDesignAsync_Throws_When_Action_Invalid()
        {
            var d = await _db.OrderDetails.Include(x => x.Order).FirstAsync(x => x.OrderDetailId == 1000);
            d.Order!.SellerUserId = "sellerA";
            d.ProductionStatus = ProductionStatus.CHECK_DESIGN;
            await _db.SaveChangesAsync();

            var act = async () => await _svc.SellerApproveOrderDetailDesignAsync(1000, ProductionStatus.DRAFT, "sellerA", "x");
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*DESIGN_REDO or READY_PROD*");
        }

        [Fact]
        public async Task SellerApproveOrderDetailDesignAsync_Success_Writes_Log_Sends_Notify_And_SignalR()
        {
            var d = await _db.OrderDetails.Include(x => x.Order).FirstAsync(x => x.OrderDetailId == 1000);
            d.Order!.SellerUserId = "sellerA";
            d.ProductionStatus = ProductionStatus.CHECK_DESIGN;
            d.AssignedDesignerUserId = "designerY";
            await _db.SaveChangesAsync();

            var ok = await _svc.SellerApproveOrderDetailDesignAsync(1000, ProductionStatus.READY_PROD, "sellerA", "ok");
            ok.Should().BeTrue();

            (await _db.OrderDetails.FindAsync(1000))!.ProductionStatus.Should().Be(ProductionStatus.READY_PROD);

            var logs = await _db.OrderDetailLogs.Where(l => l.OrderDetailId == 1000).ToListAsync();
            logs.Should().NotBeEmpty();
            logs.Last().EventType.Should().Be("DESIGN_APPROVED");

            _notifyMock.Verify(n => n.CreateAndSendNotificationAsync(
                "designerY",
                It.Is<string>(msg => msg.Contains("#1000")),
                It.IsAny<string>()), Times.AtLeastOnce);

            _hubClientsMock.Verify(h => h.Group("order_100"), Times.AtLeastOnce);
            _clientProxyMock.Verify(cp => cp.SendCoreAsync(
                "OrderStatusChanged",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        // =========================================================
        // SendOrderToReadyProdAsync
        // =========================================================
        [Fact]
        public async Task SendOrderToReadyProdAsync_ReturnsFalse_When_NotFound()
        {
            var ok = await _svc.SendOrderToReadyProdAsync(999, "sellerA");
            ok.Should().BeFalse();
        }

        [Fact]
        public async Task SendOrderToReadyProdAsync_Throws_When_WrongSeller()
        {
            var act = async () => await _svc.SendOrderToReadyProdAsync(100, "wrong");
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task SendOrderToReadyProdAsync_Throws_When_Status_Not_1()
        {
            var o = await _db.Orders.FindAsync(101);
            o!.StatusOrder = 2;
            await _db.SaveChangesAsync();

            var act = async () => await _svc.SendOrderToReadyProdAsync(101, "sellerB");
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*StatusOrder = 1*");
        }

        [Fact]
        public async Task SendOrderToReadyProdAsync_Success_Updates_Order_And_Details_And_Sends_SignalR()
        {
            var o = await _db.Orders.Include(x => x.OrderDetails).FirstAsync(x => x.OrderId == 101);
            o.StatusOrder = 1;
            await _db.SaveChangesAsync();

            var ok = await _svc.SendOrderToReadyProdAsync(101, "sellerB");
            ok.Should().BeTrue();

            var updated = await _db.Orders.Include(x => x.OrderDetails).FirstAsync(x => x.OrderId == 101);
            updated.StatusOrder.Should().Be(7);
            updated.OrderDetails.Should().OnlyContain(d => d.ProductionStatus == ProductionStatus.READY_PROD);

            _hubClientsMock.Verify(h => h.Group("order_101"), Times.AtLeastOnce);
            _clientProxyMock.Verify(cp => cp.SendCoreAsync(
                "OrderStatusChanged",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        // =========================================================
        // ApproveOrderForShippingAsync
        // =========================================================
        [Fact]
        public async Task ApproveOrderForShippingAsync_Fails_When_Order_NotFound()
        {
            var res = await _svc.ApproveOrderForShippingAsync(999);
            res.IsSuccess.Should().BeFalse();
            res.OrderFound.Should().BeFalse();
        }

        [Fact]
        public async Task ApproveOrderForShippingAsync_Fails_When_No_Details()
        {
            var o = new Order { OrderId = 202, OrderCode = "ORD202", SellerUserId = "a", StatusOrder = 1, EndCustomerId = 1, ToDistrictId = 1, ToWardCode = "W1" };
            _db.Orders.Add(o);
            await _db.SaveChangesAsync();

            var res = await _svc.ApproveOrderForShippingAsync(202);
            res.IsSuccess.Should().BeFalse();
            res.CanApprove.Should().BeFalse();
            res.ErrorMessage.Should().ContainEquivalentOf("no product details");
        }

        [Fact]
        public async Task ApproveOrderForShippingAsync_Fails_When_Customer_Missing()
        {
            var o = new Order { OrderId = 203, OrderCode = "ORD203", SellerUserId = "a", StatusOrder = 1, EndCustomerId = 999, ToDistrictId = 1, ToWardCode = "W1" };
            _db.Orders.Add(o);
            _db.OrderDetails.Add(new OrderDetail { OrderDetailId = 3000, OrderId = 203, ProductVariantId = 10, Quantity = 1, ProductionStatus = ProductionStatus.QC_DONE });
            await _db.SaveChangesAsync();

            var res = await _svc.ApproveOrderForShippingAsync(203);
            res.IsSuccess.Should().BeFalse();
            res.ErrorMessage.Should().Contain("Customer information is missing.");
        }

        [Fact]
        public async Task ApproveOrderForShippingAsync_Fails_When_DistrictOrWard_Missing()
        {
            var o = await _db.Orders.Include(x => x.OrderDetails).FirstAsync(x => x.OrderId == 100);
            o.ToDistrictId = null;
            o.ToWardCode = null;
            foreach (var d in o.OrderDetails) d.ProductionStatus = ProductionStatus.QC_DONE;
            await _db.SaveChangesAsync();

            var res = await _svc.ApproveOrderForShippingAsync(100);
            res.IsSuccess.Should().BeFalse();
            res.ErrorMessage.Should().Contain("Missing District ID or Ward Code");
        }

        [Fact]
        public async Task ApproveOrderForShippingAsync_Fails_When_Not_All_QC_Done()
        {
            var res = await _svc.ApproveOrderForShippingAsync(100);
            res.IsSuccess.Should().BeFalse();
            res.CanApprove.Should().BeFalse();
            res.ErrorMessage.Should().Contain("Not all products have passed QC");
        }

        [Fact]
        public async Task ApproveOrderForShippingAsync_Success_When_Shipping_Returns_OrderCode()
        {
            var o = await _db.Orders.Include(x => x.OrderDetails).FirstAsync(x => x.OrderId == 100);
            foreach (var d in o.OrderDetails) d.ProductionStatus = ProductionStatus.QC_DONE;
            await _db.SaveChangesAsync();

            _shippingMock
                .Setup(s => s.CreateOrderAsync(It.IsAny<CreateOrderRequest>()))
                .ReturnsAsync(new CreateOrderResult { OrderCode = "GHN123" });

            var res = await _svc.ApproveOrderForShippingAsync(100);
            res.IsSuccess.Should().BeTrue();

            var updated = await _db.Orders.Include(x => x.OrderDetails).FirstAsync(x => x.OrderId == 100);
            updated.StatusOrder.Should().Be(13);
            updated.Tracking.Should().Be("GHN123");
            updated.OrderDetails.Should().OnlyContain(d => d.ProductionStatus == ProductionStatus.SHIPPING);

            _notifyMock.Verify(n => n.CreateAndSendNotificationAsync(
                "sellerA",
                It.Is<string>(msg => msg.Contains("Mã vận đơn") || msg.Contains("GHN123")),
                It.Is<string>(url => url.Contains("/seller/orders/100"))), Times.Once);

            _hubClientsMock.Verify(h => h.Group("order_100"), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ApproveOrderForShippingAsync_When_Shipping_400_Should_Set_Status_19_And_Notify()
        {
            var o = await _db.Orders.Include(x => x.OrderDetails).FirstAsync(x => x.OrderId == 100);
            foreach (var d in o.OrderDetails) d.ProductionStatus = ProductionStatus.QC_DONE;
            await _db.SaveChangesAsync();

            _shippingMock
                .Setup(s => s.CreateOrderAsync(It.IsAny<CreateOrderRequest>()))
                .ThrowsAsync(new Exception("400 PHONE_INVALID"));

            var res = await _svc.ApproveOrderForShippingAsync(100);
            res.IsSuccess.Should().BeFalse();
            res.ErrorMessage.Should().Contain("Tạo đơn vận chuyển thất bại");

            var updated = await _db.Orders.Include(x => x.OrderDetails).FirstAsync(x => x.OrderId == 100);
            updated.StatusOrder.Should().Be(19);
            updated.OrderDetails.Should().OnlyContain(d => d.ProductionStatus == ProductionStatus.QC_DONE);

            _notifyMock.Verify(n => n.CreateAndSendNotificationAsync(
                "sellerA",
                It.Is<string>(msg => msg.Contains("PHONE") || msg.Contains("Số điện thoại", StringComparison.OrdinalIgnoreCase)),
                It.Is<string>(url => url.Contains("/seller/orders/100"))), Times.Once);
        }

        // =========================================================
        // CheckOrderCodeExistsAsync
        // =========================================================
        [Fact]
        public async Task CheckOrderCodeExistsAsync_ReturnsFalse_When_Empty()
        {
            (await _svc.CheckOrderCodeExistsAsync("")).Should().BeFalse();
            (await _svc.CheckOrderCodeExistsAsync("   ")).Should().BeFalse();
        }

        [Fact]
        public async Task CheckOrderCodeExistsAsync_ReturnsTrue_When_Exists()
        {
            (await _svc.CheckOrderCodeExistsAsync("ORD100")).Should().BeTrue();
        }
    }
}

