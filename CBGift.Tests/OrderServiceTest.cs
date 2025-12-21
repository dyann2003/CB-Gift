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

        private readonly Mock<IMapper> _mapperMock;
        private readonly MapperConfiguration _mapperConfig;
        private readonly ILogger<OrderService> _logger;

        private readonly Mock<INotificationService> _notifyMock;
        private readonly Mock<IHubContext<NotificationHub>> _hubContextMock;
        private readonly Mock<IHubClients> _hubClientsMock;
        private readonly Mock<IClientProxy> _clientProxyMock;

        private readonly Mock<IShippingService> _shippingMock;
        private readonly Mock<OrderFactory> _orderFactoryMock;
        private readonly Mock<IValidator<OrderImportRowDto>> _validatorMock;
        private readonly Mock<ReferenceDataCache> _cacheMock;

        private readonly OrderService _svc;

        // Quy ước ID theo testcase sheet:
        private const int VALID_ORDER_ID = 100;
        private const int VALID_ORDER_ID_2 = 101;
        private const int INVALID_ID = 0;
        private const int NOTFOUND_ID = 9999;

        private const int VALID_ORDERDETAIL_ID = 1000;
        private const int VALID_VARIANT_ID = 10;
        private const int NOTFOUND_VARIANT_ID = 9999;

        private const string VALID_SELLER_A = "sellerA";
        private const string VALID_SELLER_B = "sellerB";
        private const string INVALID_SELLER = "Invalid userSellerId";

        public OrderServiceTests()
        {
            _db = InMemoryDbFactory.CreateContext();
            _logger = Mock.Of<ILogger<OrderService>>();

            _mapperConfig = BuildMapperConfig();
            _mapperMock = new Mock<IMapper>(MockBehavior.Loose);
            _mapperMock.SetupGet(m => m.ConfigurationProvider).Returns(_mapperConfig);

            _notifyMock = new Mock<INotificationService>(MockBehavior.Strict);
            _notifyMock
                .Setup(n => n.CreateAndSendNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _clientProxyMock = new Mock<IClientProxy>(MockBehavior.Strict);
            _clientProxyMock
                .Setup(cp => cp.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _hubClientsMock = new Mock<IHubClients>(MockBehavior.Strict);
            _hubClientsMock.Setup(h => h.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);

            _hubContextMock = new Mock<IHubContext<NotificationHub>>(MockBehavior.Strict);
            _hubContextMock.SetupGet(h => h.Clients).Returns(_hubClientsMock.Object);

            _shippingMock = new Mock<IShippingService>(MockBehavior.Strict);

            _orderFactoryMock = new Mock<OrderFactory>(MockBehavior.Loose, null);
            _validatorMock = new Mock<IValidator<OrderImportRowDto>>(MockBehavior.Strict);

            _cacheMock = new Mock<ReferenceDataCache>(MockBehavior.Loose);
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

        // =======================
        // Mapper + Seed (giữ như bạn)
        // =======================
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
            var stDraft = new OrderStatus { StatusId = 1, Code = "DRAFT", NameVi = "Nháp" };
            var stCheckDesign = new OrderStatus { StatusId = 5, Code = "CHECKDESIGN", NameVi = "Duyệt thiết kế" };
            var stShipping = new OrderStatus { StatusId = 13, Code = "SHIPPING", NameVi = "Đang giao" };

            var p = new Product { ProductId = 1, ProductName = "Mug", ProductCode = "P001", CategoryId = 1, Status = 1, Describe = "desc" };

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
        // 1) createCustomer (UTCD01..UTCD07)
        // =========================================================
        public static IEnumerable<object[]> CreateCustomerCases()
        {
            // NOTE: “kỳ vọng exception/message” bạn chỉnh theo service thật.
            yield return new object[] { "UTCD01", "Customer Test", "0123456789", "test@example.com", true, null, null };
            yield return new object[] { "UTCD02", null, "0123456789", "test@example.com", false, typeof(ArgumentException), "name" };
            yield return new object[] { "UTCD03", "Customer Test", null, "test@example.com", false, typeof(ArgumentException), "phone" };
            yield return new object[] { "UTCD04", "Customer Test", "123", "test@example.com", false, typeof(ArgumentException), "phone" };
            yield return new object[] { "UTCD05", "Customer Test", "0123456789", "invalid_email_format@gmail", false, typeof(ArgumentException), "email" };
            yield return new object[] { "UTCD06", "Customer Test", "0123456789", null, false, typeof(ArgumentException), "email" };
            yield return new object[] { "UTCD07", "Customer Test", "0123456789", "test@example.com", true, null, null }; // duplicate-valid (theo sheet vẫn pass)
        }

        [Theory]
        [MemberData(nameof(CreateCustomerCases))]
        public async Task CreateCustomerAsync_Should_Follow_Testcase(
            string tcId,
            string name,
            string phone,
            string email,
            bool expectSuccess,
            Type expectExceptionType,
            string messageKeyword)
        {
            var req = new EndCustomerCreateRequest
            {
                Name = name,
                Email = email,
                Phone = phone,
                Address = "Valid address",
                Address1 = "Valid address1",
                ZipCode = "Valid zipCode",
                ShipState = "Valid state",
                ShipCity = "Valid city",
                ShipCountry = "Valid country"
            };

            if (expectSuccess)
            {
                var res = await _svc.CreateCustomerAsync(req);
                res.Should().NotBeNull(because: tcId);
                res.CustId.Should().BeGreaterThan(0, because: tcId);
                res.Name.Should().Be(name, because: tcId);
                return;
            }

            Func<Task> act = async () => await _svc.CreateCustomerAsync(req);
            var ex = await act.Should().ThrowAsync<Exception>(because: tcId);

            if (expectExceptionType != null)
                ex.Which.Should().BeOfType(expectExceptionType, because: tcId);

            if (!string.IsNullOrWhiteSpace(messageKeyword))
                ex.Which.Message.Should().Contain(messageKeyword, because: tcId);
        }

        // =========================================================
        // 2) createOrder (UTCD01..UTCD05)
        // Sheet của bạn thể hiện validate: endCustomerId, totalCost, sellerUserId, activeTTS
        // Nhưng method service của bạn đang: CreateOrderAsync(OrderCreateRequest req, string sellerId)
        // => Map testcase sang input: sellerId + ActiveTTS + (nếu req có EndCustomerId/TotalCost thì set)
        // =========================================================
        public static IEnumerable<object[]> CreateOrderCases()
        {
            yield return new object[] { "UTCD01", VALID_SELLER_A, true, true, null, null }; // Normal
            yield return new object[] { "UTCD02", null, true, false, typeof(ArgumentException), "seller" }; // seller required
            yield return new object[] { "UTCD03", VALID_SELLER_A, false, true, null, null }; // Normal (no TTS)
            yield return new object[] { "UTCD04", VALID_SELLER_A, true, true, null, null }; // Normal (nếu sheet có totalCost valid)
            yield return new object[] { "UTCD05", VALID_SELLER_A, true, true, null, null };
        }

        [Theory]
        [MemberData(nameof(CreateOrderCases))]
        public async Task CreateOrderAsync_Should_Follow_Testcase(
            string tcId,
            string sellerUserId,
            bool activeTts,
            bool expectSuccess,
            Type expectExceptionType,
            string messageKeyword)
        {
            var req = new OrderCreateRequest { ActiveTTS = activeTts };

            _mapperMock.Setup(m => m.Map<Order>(It.IsAny<OrderCreateRequest>()))
                .Returns((OrderCreateRequest r) => new Order
                {
                    ActiveTts = r.ActiveTTS,
                    TotalCost = 0
                });

            if (expectSuccess)
            {
                var id = await _svc.CreateOrderAsync(req, sellerUserId);
                id.Should().BeGreaterThan(0, because: tcId);

                var inDb = await _db.Orders.FindAsync(id);
                inDb.Should().NotBeNull(because: tcId);

                // Theo code test cũ của bạn:
                inDb!.StatusOrder.Should().Be(1, because: tcId);
                inDb.ProductionStatus.Should().Be("CREATED", because: tcId);

                // surcharge TTS
                inDb.TotalCost.Should().Be(activeTts ? 1 : 0, because: tcId);
                return;
            }

            Func<Task> act = async () => await _svc.CreateOrderAsync(req, sellerUserId);
            var ex = await act.Should().ThrowAsync<Exception>(because: tcId);

            if (expectExceptionType != null)
                ex.Which.Should().BeOfType(expectExceptionType, because: tcId);

            if (!string.IsNullOrWhiteSpace(messageKeyword))
                ex.Which.Message.Should().Contain(messageKeyword, because: tcId);
        }

        // =========================================================
        // 3) addOrderDetail (UTCD01..UTCD09)
        // =========================================================
        public static IEnumerable<object[]> AddOrderDetailCases()
        {
            // Map theo sheet: orderId: valid / 0 / notfound ; variantId: valid / 0 / notfound ; quantity valid/bad
            yield return new object[] { "UTCD01", VALID_ORDER_ID, VALID_SELLER_A, VALID_VARIANT_ID, 10, true, null, null };   // Normal
            yield return new object[] { "UTCD02", INVALID_ID, VALID_SELLER_A, VALID_VARIANT_ID, 10, false, typeof(ArgumentException), "order" };
            yield return new object[] { "UTCD03", NOTFOUND_ID, VALID_SELLER_A, VALID_VARIANT_ID, 10, false, typeof(Exception), "not found" };
            yield return new object[] { "UTCD04", VALID_ORDER_ID, VALID_SELLER_A, INVALID_ID, 10, false, typeof(ArgumentException), "variant" };
            yield return new object[] { "UTCD05", VALID_ORDER_ID, VALID_SELLER_A, NOTFOUND_VARIANT_ID, 10, false, typeof(ArgumentException), "does not exist" };
            yield return new object[] { "UTCD06", VALID_ORDER_ID, VALID_SELLER_A, VALID_VARIANT_ID, 0, false, typeof(ArgumentException), "greater than zero" };
            yield return new object[] { "UTCD07", VALID_ORDER_ID, "wrongSeller", VALID_VARIANT_ID, 10, false, typeof(Exception), "not yours" };
            yield return new object[] { "UTCD08", VALID_ORDER_ID, VALID_SELLER_A, VALID_VARIANT_ID, 1, true, null, null };   // Boundary-ish
            yield return new object[] { "UTCD09", VALID_ORDER_ID, VALID_SELLER_A, VALID_VARIANT_ID, 20, true, null, null };   // Large qty (nếu service cho phép)
        }

        [Theory]
        [MemberData(nameof(AddOrderDetailCases))]
        public async Task AddOrderDetailAsync_Should_Follow_Testcase(
            string tcId,
            int orderId,
            string sellerUserId,
            int variantId,
            int quantity,
            bool expectSuccess,
            Type expectExceptionType,
            string messageKeyword)
        {
            var req = new OrderDetailCreateRequest { ProductVariantID = variantId, Quantity = quantity };

            _mapperMock.Setup(m => m.Map<OrderDetail>(It.IsAny<OrderDetailCreateRequest>()))
                .Returns((OrderDetailCreateRequest r) => new OrderDetail
                {
                    ProductVariantId = r.ProductVariantID,
                    Quantity = r.Quantity,
                    NeedDesign = false
                });

            if (expectSuccess)
            {
                await _svc.AddOrderDetailAsync(orderId, req, sellerUserId);
                var order = await _db.Orders.FindAsync(orderId);
                order.Should().NotBeNull(because: tcId);
                return;
            }

            Func<Task> act = async () => await _svc.AddOrderDetailAsync(orderId, req, sellerUserId);
            var ex = await act.Should().ThrowAsync<Exception>(because: tcId);

            if (expectExceptionType != null)
                ex.Which.Should().BeOfType(expectExceptionType, because: tcId);

            if (!string.IsNullOrWhiteSpace(messageKeyword))
                ex.Which.Message.Should().Contain(messageKeyword, because: tcId);
        }

        // =========================================================
        // 4) deleteOrder (UTCD01..UTCD04)
        // =========================================================
        public static IEnumerable<object[]> DeleteOrderCases()
        {
            yield return new object[] { "UTCD01", VALID_ORDER_ID, VALID_SELLER_A, true, null, null }; // true
            yield return new object[] { "UTCD02", INVALID_ID, VALID_SELLER_A, false, null, null }; // false / exception tùy service
            yield return new object[] { "UTCD03", NOTFOUND_ID, VALID_SELLER_A, false, null, null }; // false
            yield return new object[] { "UTCD04", VALID_ORDER_ID_2, null, false, typeof(ArgumentException), "seller" }; // seller required
        }

        [Theory]
        [MemberData(nameof(DeleteOrderCases))]
        public async Task DeleteOrderAsync_Should_Follow_Testcase(
            string tcId,
            int orderId,
            string sellerUserId,
            bool expectTrue,
            Type expectExceptionType,
            string messageKeyword)
        {
            if (expectExceptionType == null)
            {
                var ok = await _svc.DeleteOrderAsync(orderId, sellerUserId);
                ok.Should().Be(expectTrue, because: tcId);
                return;
            }

            Func<Task> act = async () => await _svc.DeleteOrderAsync(orderId, sellerUserId);
            var ex = await act.Should().ThrowAsync<Exception>(because: tcId);
            ex.Which.Should().BeOfType(expectExceptionType, because: tcId);

            if (!string.IsNullOrWhiteSpace(messageKeyword))
                ex.Which.Message.Should().Contain(messageKeyword, because: tcId);
        }

        // =========================================================
        // 5) sellerApproveOrderDesign (UTCD01..UTCD07)
        // =========================================================
        public static IEnumerable<object[]> SellerApproveOrderDesignCases()
        {
            yield return new object[] { "UTCD01", VALID_ORDER_ID, ProductionStatus.READY_PROD, VALID_SELLER_A, true, null, null };
            yield return new object[] { "UTCD02", INVALID_ID, ProductionStatus.READY_PROD, VALID_SELLER_A, false, null, null };
            yield return new object[] { "UTCD03", NOTFOUND_ID, ProductionStatus.READY_PROD, VALID_SELLER_A, false, null, null };
            yield return new object[] { "UTCD04", VALID_ORDER_ID, ProductionStatus.READY_PROD, null, false, typeof(ArgumentException), "seller" };
            yield return new object[] { "UTCD05", VALID_ORDER_ID, ProductionStatus.DRAFT, VALID_SELLER_A, false, typeof(InvalidOperationException), "" }; // action invalid
            yield return new object[] { "UTCD06", VALID_ORDER_ID, ProductionStatus.READY_PROD, "other", false, typeof(UnauthorizedAccessException), "not authorized" };
            yield return new object[] { "UTCD07", VALID_ORDER_ID, ProductionStatus.READY_PROD, VALID_SELLER_A, true, null, null };
        }

        [Theory]
        [MemberData(nameof(SellerApproveOrderDesignCases))]
        public async Task SellerApproveOrderDesignAsync_Should_Follow_Testcase(
            string tcId,
            int orderId,
            ProductionStatus action,
            string sellerUserId,
            bool expectTrue,
            Type expectExceptionType,
            string messageKeyword)
        {
            // Đảm bảo order 100 đang ở CHECKDESIGN + details CHECK_DESIGN để case success chạy đúng.
            if (orderId == VALID_ORDER_ID && expectTrue)
            {
                var o = await _db.Orders.Include(x => x.OrderDetails).FirstAsync(x => x.OrderId == VALID_ORDER_ID);
                o.StatusOrder = 5;
                foreach (var d in o.OrderDetails)
                {
                    d.NeedDesign = true;
                    d.ProductionStatus = ProductionStatus.CHECK_DESIGN;
                    d.AssignedDesignerUserId = "designerX";
                }
                await _db.SaveChangesAsync();
            }

            if (expectExceptionType == null)
            {
                var ok = await _svc.SellerApproveOrderDesignAsync(orderId, action, sellerUserId);
                ok.Should().Be(expectTrue, because: tcId);
                return;
            }

            Func<Task> act = async () => await _svc.SellerApproveOrderDesignAsync(orderId, action, sellerUserId);
            var ex = await act.Should().ThrowAsync<Exception>(because: tcId);

            ex.Which.Should().BeOfType(expectExceptionType, because: tcId);
            if (!string.IsNullOrWhiteSpace(messageKeyword))
                ex.Which.Message.Should().Contain(messageKeyword, because: tcId);
        }

        // =========================================================
        // 6) sellerApproveOrderDetailDesign (UTCD01..UTCD07)
        // =========================================================
        public static IEnumerable<object[]> SellerApproveOrderDetailDesignCases()
        {
            yield return new object[] { "UTCD01", VALID_ORDERDETAIL_ID, ProductionStatus.READY_PROD, VALID_SELLER_A, "ok", true, null, null };
            yield return new object[] { "UTCD02", INVALID_ID, ProductionStatus.READY_PROD, VALID_SELLER_A, "ok", false, null, null };
            yield return new object[] { "UTCD03", NOTFOUND_ID, ProductionStatus.READY_PROD, VALID_SELLER_A, "ok", false, null, null };
            yield return new object[] { "UTCD04", VALID_ORDERDETAIL_ID, ProductionStatus.READY_PROD, null, "ok", false, typeof(ArgumentException), "seller" };
            yield return new object[] { "UTCD05", VALID_ORDERDETAIL_ID, ProductionStatus.DRAFT, VALID_SELLER_A, "ok", false, typeof(InvalidOperationException), "DESIGN_REDO" };
            yield return new object[] { "UTCD06", VALID_ORDERDETAIL_ID, ProductionStatus.READY_PROD, "wrong", "ok", false, typeof(UnauthorizedAccessException), "" };
            yield return new object[] { "UTCD07", VALID_ORDERDETAIL_ID, ProductionStatus.READY_PROD, VALID_SELLER_A, "ok", true, null, null };
        }

        [Theory]
        [MemberData(nameof(SellerApproveOrderDetailDesignCases))]
        public async Task SellerApproveOrderDetailDesignAsync_Should_Follow_Testcase(
            string tcId,
            int orderDetailId,
            ProductionStatus action,
            string sellerUserId,
            string reason,
            bool expectTrue,
            Type expectExceptionType,
            string messageKeyword)
        {
            // Setup để case success chạy đúng: detail phải CHECK_DESIGN, seller phải owner
            if (orderDetailId == VALID_ORDERDETAIL_ID && expectTrue)
            {
                var d = await _db.OrderDetails.Include(x => x.Order).FirstAsync(x => x.OrderDetailId == VALID_ORDERDETAIL_ID);
                d.Order!.SellerUserId = VALID_SELLER_A;
                d.ProductionStatus = ProductionStatus.CHECK_DESIGN;
                d.AssignedDesignerUserId = "designerY";
                await _db.SaveChangesAsync();
            }

            if (expectExceptionType == null)
            {
                var ok = await _svc.SellerApproveOrderDetailDesignAsync(orderDetailId, action, sellerUserId, reason);
                ok.Should().Be(expectTrue, because: tcId);
                return;
            }

            Func<Task> act = async () => await _svc.SellerApproveOrderDetailDesignAsync(orderDetailId, action, sellerUserId, reason);
            var ex = await act.Should().ThrowAsync<Exception>(because: tcId);

            ex.Which.Should().BeOfType(expectExceptionType, because: tcId);
            if (!string.IsNullOrWhiteSpace(messageKeyword))
                ex.Which.Message.Should().Contain(messageKeyword, because: tcId);
        }

        // =========================================================
        // 7) sendOrderToReadyProd (UTCD01..UTCD05)
        // =========================================================
        public static IEnumerable<object[]> SendOrderToReadyProdCases()
        {
            yield return new object[] { "UTCD01", VALID_ORDER_ID_2, VALID_SELLER_B, true, null, null };
            yield return new object[] { "UTCD02", INVALID_ID, VALID_SELLER_B, false, null, null };
            yield return new object[] { "UTCD03", NOTFOUND_ID, VALID_SELLER_B, false, null, null };
            yield return new object[] { "UTCD04", VALID_ORDER_ID_2, INVALID_SELLER, false, typeof(UnauthorizedAccessException), "" };
            yield return new object[] { "UTCD05", VALID_ORDER_ID_2, null, false, typeof(ArgumentException), "seller" };
        }

        [Theory]
        [MemberData(nameof(SendOrderToReadyProdCases))]
        public async Task SendOrderToReadyProdAsync_Should_Follow_Testcase(
            string tcId,
            int orderId,
            string sellerUserId,
            bool expectTrue,
            Type expectExceptionType,
            string messageKeyword)
        {
            // đảm bảo order 101 đúng status = 1 để success
            if (orderId == VALID_ORDER_ID_2 && expectTrue)
            {
                var o = await _db.Orders.FindAsync(VALID_ORDER_ID_2);
                o!.StatusOrder = 1;
                await _db.SaveChangesAsync();
            }

            if (expectExceptionType == null)
            {
                var ok = await _svc.SendOrderToReadyProdAsync(orderId, sellerUserId);
                ok.Should().Be(expectTrue, because: tcId);
                return;
            }

            Func<Task> act = async () => await _svc.SendOrderToReadyProdAsync(orderId, sellerUserId);
            var ex = await act.Should().ThrowAsync<Exception>(because: tcId);

            ex.Which.Should().BeOfType(expectExceptionType, because: tcId);
            if (!string.IsNullOrWhiteSpace(messageKeyword))
                ex.Which.Message.Should().Contain(messageKeyword, because: tcId);
        }

        // =========================================================
        // 8) approveOrderForShipping (UTCD01..UTCD03)
        // =========================================================
        public static IEnumerable<object[]> ApproveOrderForShippingCases()
        {
            yield return new object[] { "UTCD01", VALID_ORDER_ID, true };
            yield return new object[] { "UTCD02", INVALID_ID, false };
            yield return new object[] { "UTCD03", NOTFOUND_ID, false };
        }

        [Theory]
        [MemberData(nameof(ApproveOrderForShippingCases))]
        public async Task ApproveOrderForShippingAsync_Should_Follow_Testcase(string tcId, int orderId, bool expectCanFind)
        {
            if (orderId == VALID_ORDER_ID)
            {
                // setup để success: QC_DONE + shipping mock trả orderCode
                var o = await _db.Orders.Include(x => x.OrderDetails).FirstAsync(x => x.OrderId == VALID_ORDER_ID);
                foreach (var d in o.OrderDetails) d.ProductionStatus = ProductionStatus.QC_DONE;
                await _db.SaveChangesAsync();

                _shippingMock
                    .Setup(s => s.CreateOrderAsync(It.IsAny<CreateOrderRequest>()))
                    .ReturnsAsync(new CreateOrderResult { OrderCode = "GHN123" });
            }

            var res = await _svc.ApproveOrderForShippingAsync(orderId);

            res.OrderFound.Should().Be(expectCanFind, because: tcId);

            if (expectCanFind)
            {
                // theo flow trong test cũ của bạn: nếu ok thì StatusOrder=13 + Tracking
                res.IsSuccess.Should().BeTrue(because: tcId);

                var updated = await _db.Orders.Include(x => x.OrderDetails).FirstAsync(x => x.OrderId == orderId);
                updated.StatusOrder.Should().Be(13, because: tcId);
                updated.Tracking.Should().Be("GHN123", because: tcId);
            }
            else
            {
                res.IsSuccess.Should().BeFalse(because: tcId);
            }
        }
    }
}

