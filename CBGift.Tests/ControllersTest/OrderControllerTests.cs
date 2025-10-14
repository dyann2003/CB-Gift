using CB_Gift.Controllers;
using CB_Gift.DTOs;
using CB_Gift.Models;
using CB_Gift.Services.IService;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace CB_Gift.Tests.Controllers
{
    public class OrderControllerTests
    {
        private static void LogResult(string testName, IActionResult result)
        {
            Console.WriteLine($"[{testName}] => ResultType: {result.GetType().Name}");
        }

        private static OrderController CreateControllerWithUser(IOrderService service, string? sellerId = "SELLER123")
        {
            var controller = new OrderController(service)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            if (sellerId != null)
            {
                var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, sellerId) };
                controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));
            }

            return controller;
        }

        // ---------- 1. GetAllOrders ----------
        [Fact]
        public async Task GetAllOrders_Returns_Ok_With_Data()
        {
            var orders = new List<Order> { new Order { OrderId = 1 }, new Order { OrderId = 2 } };
            var mockService = new Mock<IOrderService>();
            mockService.Setup(s => s.GetAllOrders()).ReturnsAsync(orders);

            var ctl = CreateControllerWithUser(mockService.Object);

            var r = await ctl.GetAllOrders();
            LogResult("GetAllOrders", r);

            var ok = r.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(orders);
        }

        [Fact]
        public async Task GetAllOrders_Returns_Empty_List()
        {
            var mockService = new Mock<IOrderService>();
            mockService.Setup(s => s.GetAllOrders()).ReturnsAsync(new List<Order>());

            var ctl = CreateControllerWithUser(mockService.Object);

            var r = await ctl.GetAllOrders();
            LogResult("GetAllOrders_Empty", r);

            var ok = r.Should().BeOfType<OkObjectResult>().Subject;
            ((List<Order>)ok.Value!).Should().BeEmpty();
        }
        [Fact]
        public async Task GetAllOrders_Calls_Service_Once_And_No_Other_Calls()
        {
            var mock = new Mock<IOrderService>();
            mock.Setup(s => s.GetAllOrders()).ReturnsAsync(new List<Order> { new Order { OrderId = 1 } });

            var ctl = CreateControllerWithUser(mock.Object);

            var r = await ctl.GetAllOrders();
            LogResult("GetAllOrders_VerifyCalls", r);

            r.Should().BeOfType<OkObjectResult>();
            mock.Verify(s => s.GetAllOrders(), Times.Once);
            mock.VerifyNoOtherCalls();
        }
        //api thiếu try-catch
        [Fact]
        public async Task GetAllOrders_Returns_BadRequest_When_Service_Throws() // RED
        {
            var mock = new Mock<IOrderService>();
            mock.Setup(s => s.GetAllOrders())
                .ThrowsAsync(new InvalidOperationException("boom"));

            var ctl = CreateControllerWithUser(mock.Object);

            var r = await ctl.GetAllOrders();               // hiện tại sẽ ném exception (fail)
            LogResult("GetAllOrders_ServiceThrows", r);

            var bad = r.Should().BeOfType<BadRequestObjectResult>().Subject; // mong 400
            bad.Value.Should().Be("boom");
        }


        //re-check
        //[Fact]
        //public async Task GetAllOrders_Returns_Ok_With_Null_When_Service_Returns_Null()
        //{
        //    var mock = new Mock<IOrderService>();
        //    mock.Setup(s => s.GetAllOrders()).ReturnsAsync((List<Order>)null!);

        //    var ctl = CreateControllerWithUser(mock.Object);

        //    var r = await ctl.GetAllOrders();
        //    LogResult("GetAllOrders_Null", r);

        //    var ok = r.Should().BeOfType<OkObjectResult>().Subject;
        //    ok.Value.Should().BeNull(); 
        //}

        [Fact]
        public async Task GetAllOrders_Propagates_Exception_From_Service()
        {
            var mock = new Mock<IOrderService>();
            mock.Setup(s => s.GetAllOrders()).ThrowsAsync(new InvalidOperationException("boom"));

            var ctl = CreateControllerWithUser(mock.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => ctl.GetAllOrders());
            Console.WriteLine($"[GetAllOrders_Exception] => {ex.GetType().Name}: {ex.Message}");
        }

        [Fact]
        public async Task GetAllOrders_Returns_All_Items_For_Large_List()
        {
            var big = Enumerable.Range(1, 1_000).Select(i => new Order { OrderId = i }).ToList();
            var mock = new Mock<IOrderService>();
            mock.Setup(s => s.GetAllOrders()).ReturnsAsync(big);

            var ctl = CreateControllerWithUser(mock.Object);

            var r = await ctl.GetAllOrders();
            LogResult("GetAllOrders_BigList", r);

            var ok = r.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(big);
        }


        // ---------- 2. GetOrderWithDetails ----------
        [Fact]
        public async Task GetOrderWithDetails_Returns_Ok_When_Found()
        {
            var mockService = new Mock<IOrderService>();
            var dto = new OrderWithDetailsDto { OrderId = 1 };
            mockService.Setup(s => s.GetOrderDetailAsync(1, null)).ReturnsAsync(dto);

            var ctl = CreateControllerWithUser(mockService.Object);
            var r = await ctl.GetOrderWithDetails(1);
            LogResult("GetOrderWithDetails_Found", r);

            var ok = r.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().Be(dto);
        }

        [Fact]
        public async Task GetOrderWithDetails_Returns_NotFound_When_Null()
        {
            var mockService = new Mock<IOrderService>();
            mockService.Setup(s => s.GetOrderDetailAsync(99, null)).ReturnsAsync((OrderWithDetailsDto?)null);

            var ctl = CreateControllerWithUser(mockService.Object);
            var r = await ctl.GetOrderWithDetails(99);
            LogResult("GetOrderWithDetails_NotFound", r);

            r.Should().BeOfType<NotFoundObjectResult>();
        }
        [Fact]
        public async Task GetOrderWithDetails_Calls_Service_With_Correct_Params()
        {
            var mockService = new Mock<IOrderService>();
            var dto = new OrderWithDetailsDto { OrderId = 42 };
            mockService.Setup(s => s.GetOrderDetailAsync(42, null)).ReturnsAsync(dto);

            var ctl = CreateControllerWithUser(mockService.Object);

            var r = await ctl.GetOrderWithDetails(42);
            LogResult("GetOrderWithDetails_VerifyParams", r);

            r.Should().BeOfType<OkObjectResult>();
            mockService.Verify(s => s.GetOrderDetailAsync(42, null), Times.Once);
            mockService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetOrderWithDetails_NotFound_Includes_Message()
        {
            var mockService = new Mock<IOrderService>();
            mockService.Setup(s => s.GetOrderDetailAsync(99, null))
                       .ReturnsAsync((OrderWithDetailsDto?)null);

            var ctl = CreateControllerWithUser(mockService.Object);

            var r = await ctl.GetOrderWithDetails(99);
            LogResult("GetOrderWithDetails_NotFound_Message", r);

            var nf = r.Should().BeOfType<NotFoundObjectResult>().Subject;
            nf.Value.Should().Be("Order not found");
        }

        [Fact]
        public async Task GetOrderWithDetails_Returns_BadRequest_When_Service_Throws() // RED
        {
            var mock = new Mock<IOrderService>();
            mock.Setup(s => s.GetOrderDetailAsync(7, null))
                .ThrowsAsync(new Exception("db down"));

            var ctl = CreateControllerWithUser(mock.Object);

            var r = await ctl.GetOrderWithDetails(7);       // hiện tại bubble exception (fail)
            LogResult("GetOrderWithDetails_ServiceThrows", r);

            var bad = r.Should().BeOfType<BadRequestObjectResult>().Subject;
            bad.Value.Should().Be("db down");
        }


        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetOrderWithDetails_Should_Return_BadRequest_When_OrderId_Invalid(int orderId)// failed
        {
            // Arrange
            var mockService = new Mock<IOrderService>();
            var ctl = CreateControllerWithUser(mockService.Object);

            // Act
            var r = await ctl.GetOrderWithDetails(orderId);
            LogResult($"GetOrderWithDetails_Invalid_{orderId}", r);

            // Assert
            var bad = r.Should().BeOfType<BadRequestObjectResult>().Subject;
            bad.Value.Should().Be("Invalid order id");

            // Đồng thời: service KHÔNG được gọi
            mockService.Verify(s => s.GetOrderDetailAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }


        [Theory]
        [InlineData(1)]
        [InlineData(123)]
        [InlineData(999)]
        public async Task GetOrderWithDetails_Returns_Ok_For_Existing_Orders(int orderId)
        {
            var mockService = new Mock<IOrderService>();
            var dto = new OrderWithDetailsDto { OrderId = orderId };

            mockService.Setup(s => s.GetOrderDetailAsync(orderId, null))
                       .ReturnsAsync(dto);

            var ctl = CreateControllerWithUser(mockService.Object);

            var r = await ctl.GetOrderWithDetails(orderId);
            LogResult($"GetOrderWithDetails_Found_{orderId}", r);

            var ok = r.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().Be(dto);
        }

        // ---------- 3. CreateEndCustomer ----------
        [Fact]
        public async Task CreateEndCustomer_Returns_Ok_With_Result()
        {
            var request = new EndCustomerCreateRequest { Name = "John" };
            var customer = new EndCustomer { CustId = 10, Name = "John" };
            var mockService = new Mock<IOrderService>();
            mockService.Setup(s => s.CreateCustomerAsync(request)).ReturnsAsync(customer);

            var ctl = CreateControllerWithUser(mockService.Object);
            var r = await ctl.CreateEndCustomer(request);
            LogResult("CreateEndCustomer_Happy", r);

            var ok = r.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().Be(customer);
        }

        [Fact]
        public async Task CreateEndCustomer_Returns_BadRequest_On_Exception()
        {
            var mockService = new Mock<IOrderService>();
            mockService.Setup(s => s.CreateCustomerAsync(It.IsAny<EndCustomerCreateRequest>()))
                       .ThrowsAsync(new Exception("failed"));

            var ctl = CreateControllerWithUser(mockService.Object);
            var ex = await Record.ExceptionAsync(() => ctl.CreateEndCustomer(new EndCustomerCreateRequest()));
            ex.Should().NotBeNull();
        }
        // 3.1 Verify service được gọi đúng tham số & đúng 1 lần
        [Fact]
        public async Task CreateEndCustomer_Calls_Service_Once_With_Request()
        {
            var request = new EndCustomerCreateRequest { Name = "John" };
            var customer = new EndCustomer { CustId = 10, Name = "John" };
            var mockService = new Mock<IOrderService>();
            mockService.Setup(s => s.CreateCustomerAsync(request)).ReturnsAsync(customer);

            var ctl = CreateControllerWithUser(mockService.Object);

            var r = await ctl.CreateEndCustomer(request);
            LogResult("CreateEndCustomer_VerifyCalls", r);

            var ok = r.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().Be(customer);

            mockService.Verify(s => s.CreateCustomerAsync(request), Times.Once);
            mockService.VerifyNoOtherCalls();
        }

        // 3.2 ModelState invalid -> phải trả 400 và KHÔNG gọi service
        [Fact]
        public async Task CreateEndCustomer_Returns_BadRequest_When_ModelInvalid() // RED
        {
            var req = new EndCustomerCreateRequest { Name = "" }; // thiếu dữ liệu
            var mock = new Mock<IOrderService>();

            var ctl = CreateControllerWithUser(mock.Object);
            ctl.ModelState.AddModelError("Name", "Required"); // giả lập [ApiController] validation

            var r = await ctl.CreateEndCustomer(req); // hiện tại controller vẫn Ok(...) => test này sẽ FAIL
            LogResult("CreateEndCustomer_ModelInvalid", r);

            r.Should().BeOfType<BadRequestObjectResult>(); // mong 400
            mock.Verify(s => s.CreateCustomerAsync(It.IsAny<EndCustomerCreateRequest>()), Times.Never);
        }

        // 3.3 request == null -> phải trả 400 với thông điệp rõ ràng và KHÔNG gọi service
        [Fact]
        public async Task CreateEndCustomer_Returns_BadRequest_When_Request_Is_Null() // RED
        {
            var mock = new Mock<IOrderService>();
            var ctl = CreateControllerWithUser(mock.Object);

            var r = await ctl.CreateEndCustomer(null!); // hiện tại controller ném exception => test này sẽ FAIL
            LogResult("CreateEndCustomer_NullRequest", r);

            var bad = r.Should().BeOfType<BadRequestObjectResult>().Subject;
            bad.Value.Should().Be("Request body cannot be null."); // ép dev chuẩn hoá message
            mock.Verify(s => s.CreateCustomerAsync(It.IsAny<EndCustomerCreateRequest>()), Times.Never);
        }

        // 3.4 Service ném lỗi domain -> controller phải catch và trả 400 với message
        [Fact]
        public async Task CreateEndCustomer_Returns_BadRequest_When_Service_Throws() // RED
        {
            var req = new EndCustomerCreateRequest { Name = "John" };
            var mock = new Mock<IOrderService>();
            mock.Setup(s => s.CreateCustomerAsync(req))
                .ThrowsAsync(new InvalidOperationException("duplicate"));

            var ctl = CreateControllerWithUser(mock.Object);

            var r = await ctl.CreateEndCustomer(req); // hiện tại controller bubble exception => test này sẽ FAIL
            LogResult("CreateEndCustomer_ServiceThrows", r);

            var bad = r.Should().BeOfType<BadRequestObjectResult>().Subject;
            bad.Value.Should().Be("duplicate"); // ép dev trả message từ exception
            mock.Verify(s => s.CreateCustomerAsync(req), Times.Once);
        }


        // ---------- 4. CreateOrder ----------
        [Fact]
        public async Task CreateOrder_Returns_Ok_When_Success()
        {
            var req = new OrderCreateRequest { EndCustomerID = 10 };
            var mockService = new Mock<IOrderService>();
            mockService.Setup(s => s.CreateOrderAsync(req, "SELLER123")).ReturnsAsync(101);

            var ctl = CreateControllerWithUser(mockService.Object);
            var r = await ctl.CreateOrder(req);
            LogResult("CreateOrder_Success", r);

            var ok = r.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(new { OrderId = 101, Message = "Order created successfully" });
        }

        [Fact]
        public async Task CreateOrder_Throws_When_No_User_In_Context()
        {
            var ctl = CreateControllerWithUser(new Mock<IOrderService>().Object, sellerId: null);
            await Assert.ThrowsAsync<Exception>(() => ctl.CreateOrder(new OrderCreateRequest()));
        }
        [Fact]
        public async Task CreateOrder_Calls_Service_Once_With_Correct_Params()
        {
            var req = new OrderCreateRequest { EndCustomerID = 10 };
            var mock = new Mock<IOrderService>();
            mock.Setup(s => s.CreateOrderAsync(req, "SELLER123")).ReturnsAsync(202);

            var ctl = CreateControllerWithUser(mock.Object);

            var r = await ctl.CreateOrder(req);
            LogResult("CreateOrder_VerifyParams", r);

            var ok = r.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(new { OrderId = 202, Message = "Order created successfully" });

            mock.Verify(s => s.CreateOrderAsync(req, "SELLER123"), Times.Once);
            mock.VerifyNoOtherCalls();
        }
        ///No user ⇒ phải trả Unauthorized & không gọi service
        [Fact]
        public async Task CreateOrder_Returns_Unauthorized_When_No_User_In_Context() // RED
        {
            var mock = new Mock<IOrderService>();
            var ctl = CreateControllerWithUser(mock.Object, sellerId: null);

            var r = await ctl.CreateOrder(new OrderCreateRequest { EndCustomerID = 1 }); //test sẽ fail
            LogResult("CreateOrder_NoUser", r);

            var unauth = r.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauth.Value.Should().Be("SellerUserId not found.");

            mock.Verify(s => s.CreateOrderAsync(It.IsAny<OrderCreateRequest>(), It.IsAny<string>()), Times.Never);
        }
        //ModelState invalid ⇒ phải BadRequest
        [Fact]
        public async Task CreateOrder_Returns_BadRequest_When_ModelInvalid() // RED
        {
            var req = new OrderCreateRequest(); // thiếu EndCustomerID 
            var mock = new Mock<IOrderService>();

            var ctl = CreateControllerWithUser(mock.Object);
            ctl.ModelState.AddModelError("EndCustomerID", "Required");

            var r = await ctl.CreateOrder(req); // hiện tại controller vẫn Ok(...) => fail
            LogResult("CreateOrder_ModelInvalid", r);

            r.Should().BeOfType<BadRequestObjectResult>();
            mock.Verify(s => s.CreateOrderAsync(It.IsAny<OrderCreateRequest>(), It.IsAny<string>()), Times.Never);
        }
        //Request null
        [Fact]
        public async Task CreateOrder_Returns_BadRequest_When_Request_Is_Null() // RED
        {
            var mock = new Mock<IOrderService>();
            var ctl = CreateControllerWithUser(mock.Object);

            var r = await ctl.CreateOrder(null!); // hiện tại thường ném ArgumentNullException -> sẽ FAIL
            LogResult("CreateOrder_NullRequest", r);

            var bad = r.Should().BeOfType<BadRequestObjectResult>().Subject;
            bad.Value.Should().Be("Request body cannot be null.");
            mock.Verify(s => s.CreateOrderAsync(It.IsAny<OrderCreateRequest>(), It.IsAny<string>()), Times.Never);
        }
        //trycatch
        [Fact]
        public async Task CreateOrder_Returns_BadRequest_When_Service_Throws() // RED
        {
            var req = new OrderCreateRequest { EndCustomerID = 77 };
            var mock = new Mock<IOrderService>();
            mock.Setup(s => s.CreateOrderAsync(req, "SELLER123"))
                .ThrowsAsync(new InvalidOperationException("DB insert failed"));

            var ctl = CreateControllerWithUser(mock.Object);

            var r = await ctl.CreateOrder(req); // hiện tại bubble exception -> sẽ FAIL
            LogResult("CreateOrder_ServiceThrows", r);

            var bad = r.Should().BeOfType<BadRequestObjectResult>().Subject;
            bad.Value.Should().Be("DB insert failed");
            mock.Verify(s => s.CreateOrderAsync(req, "SELLER123"), Times.Once);
        }

        // ---------- 5. AddOrderDetail ----------
        [Fact]
        public async Task AddOrderDetail_Returns_Ok_When_Success()
        {
            var req = new OrderDetailCreateRequest { ProductVariantID = 5 };
            var mockService = new Mock<IOrderService>();
            mockService.Setup(s => s.AddOrderDetailAsync(1, req, "SELLER123")).Returns(Task.CompletedTask);

            var ctl = CreateControllerWithUser(mockService.Object);
            var r = await ctl.AddOrderDetail(1, req);
            LogResult("AddOrderDetail_Success", r);

            var ok = r.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(new { Message = "Order detail added successfully" });
        }

        [Fact]
        public async Task AddOrderDetail_Throws_When_No_UserId()
        {
            var ctl = CreateControllerWithUser(new Mock<IOrderService>().Object, sellerId: null);
            await Assert.ThrowsAsync<Exception>(() => ctl.AddOrderDetail(1, new OrderDetailCreateRequest()));
        }

        [Fact]
        public async Task AddOrderDetail_Returns_BadRequest_When_ModelInvalid() // RED
        {
            var mock = new Mock<IOrderService>();
            var ctl = CreateControllerWithUser(mock.Object);
            ctl.ModelState.AddModelError("ProductVariantID", "Required");

            var req = new OrderDetailCreateRequest();
            var r = await ctl.AddOrderDetail(1, req);
            LogResult("AddOrderDetail_ModelInvalid", r);

            r.Should().BeOfType<BadRequestObjectResult>();
            mock.Verify(s => s.AddOrderDetailAsync(It.IsAny<int>(), It.IsAny<OrderDetailCreateRequest>(), It.IsAny<string>()), Times.Never);
        }
        //if (request == null) return BadRequest("Request body cannot be null.");
        [Fact]
        public async Task AddOrderDetail_Returns_BadRequest_When_Request_Null() // RED
        {
            var mock = new Mock<IOrderService>();
            var ctl = CreateControllerWithUser(mock.Object);

            var r = await ctl.AddOrderDetail(1, null!); // hiện tại ném exception => fail
            LogResult("AddOrderDetail_NullRequest", r);

            var bad = r.Should().BeOfType<BadRequestObjectResult>().Subject;
            bad.Value.Should().Be("Request body cannot be null.");
            mock.Verify(s => s.AddOrderDetailAsync(It.IsAny<int>(), It.IsAny<OrderDetailCreateRequest>(), It.IsAny<string>()), Times.Never);
        }
        //if (orderId <= 0) return BadRequest("Invalid order id");
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task AddOrderDetail_Returns_BadRequest_When_OrderId_Invalid(int invalidId) // RED
        {
            var mock = new Mock<IOrderService>();
            var ctl = CreateControllerWithUser(mock.Object);
            var req = new OrderDetailCreateRequest { ProductVariantID = 5 };

            var r = await ctl.AddOrderDetail(invalidId, req);
            LogResult($"AddOrderDetail_InvalidOrderId_{invalidId}", r);

            var bad = r.Should().BeOfType<BadRequestObjectResult>().Subject;
            bad.Value.Should().Be("Invalid order id");
            mock.Verify(s => s.AddOrderDetailAsync(It.IsAny<int>(), It.IsAny<OrderDetailCreateRequest>(), It.IsAny<string>()), Times.Never);
        }
        //try/catch trong controller.
        [Fact]
        public async Task AddOrderDetail_Returns_BadRequest_When_Service_Throws() // RED
        {
            var req = new OrderDetailCreateRequest { ProductVariantID = 10 };
            var mock = new Mock<IOrderService>();
            mock.Setup(s => s.AddOrderDetailAsync(1, req, "SELLER123"))
                .ThrowsAsync(new InvalidOperationException("Failed to add detail"));

            var ctl = CreateControllerWithUser(mock.Object);

            var r = await ctl.AddOrderDetail(1, req); // hiện tại bubble => fail
            LogResult("AddOrderDetail_ServiceThrows", r);

            var bad = r.Should().BeOfType<BadRequestObjectResult>().Subject;
            bad.Value.Should().Be("Failed to add detail");
        }
        //chưa có try/catch
        [Fact]
        public async Task AddOrderDetail_Throws_When_Service_Task_Faulted() // RED
        {
            var req = new OrderDetailCreateRequest { ProductVariantID = 99 };
            var mock = new Mock<IOrderService>();
            mock.Setup(s => s.AddOrderDetailAsync(2, req, "SELLER123"))
                .Returns(Task.FromException(new Exception("Task failed")));

            var ctl = CreateControllerWithUser(mock.Object);
            var r = await ctl.AddOrderDetail(2, req);
            LogResult("AddOrderDetail_TaskFaulted", r);

            var bad = r.Should().BeOfType<BadRequestObjectResult>().Subject;
            bad.Value.Should().Be("Task failed");
        }

        // ---------- 6. MakeOrder ----------
        [Fact]
        public async Task MakeOrder_Returns_Ok_On_Success()
        {
            var req = new MakeOrderDto { OrderCreate = new OrderCreateRequest() };
            var mockService = new Mock<IOrderService>();
            mockService.Setup(s => s.MakeOrder(req, "SELLER123")).ReturnsAsync(new MakeOrderResponse());

            var ctl = CreateControllerWithUser(mockService.Object);
            var r = await ctl.MakeOrder(req);
            LogResult("MakeOrder_Success", r);

            var ok = r.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(new { message = "Order created susscessfully!" });
        }

        [Fact]
        public async Task MakeOrder_Returns_BadRequest_On_Exception()
        {
            var req = new MakeOrderDto();
            var mockService = new Mock<IOrderService>();
            mockService.Setup(s => s.MakeOrder(req, "SELLER123"))
                       .ThrowsAsync(new Exception("something broke"));

            var ctl = CreateControllerWithUser(mockService.Object);
            var r = await ctl.MakeOrder(req);
            LogResult("MakeOrder_Exception", r);

            var bad = r.Should().BeOfType<BadRequestObjectResult>().Subject;
            bad.Value.Should().Be("something broke");
        }

        //ModelState invalid
        [Fact]
        public async Task MakeOrder_Returns_BadRequest_When_ModelInvalid() // RED
        {
            var mock = new Mock<IOrderService>();
            var ctl = CreateControllerWithUser(mock.Object);

            // Giả lập vi phạm model (thiếu OrderCreate)
            ctl.ModelState.AddModelError("OrderCreate", "Required");

            var r = await ctl.MakeOrder(new MakeOrderDto());
            LogResult("MakeOrder_ModelInvalid", r);

            r.Should().BeOfType<BadRequestObjectResult>();
            mock.Verify(s => s.MakeOrder(It.IsAny<MakeOrderDto>(), It.IsAny<string>()), Times.Never);
        }
        [Fact]
        public async Task MakeOrder_Calls_Service_Once_With_Correct_Params()
        {
            var req = new MakeOrderDto { OrderCreate = new OrderCreateRequest { EndCustomerID = 10 } };
            var mock = new Mock<IOrderService>();
            mock.Setup(s => s.MakeOrder(req, "SELLER123")).ReturnsAsync(new MakeOrderResponse());

            var ctl = CreateControllerWithUser(mock.Object);

            var r = await ctl.MakeOrder(req);
            LogResult("MakeOrder_VerifyParams", r);

            var ok = r.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(new { message = "Order created susscessfully!" });

            mock.Verify(s => s.MakeOrder(req, "SELLER123"), Times.Once);
            mock.VerifyNoOtherCalls();
        }
        //Request null
        [Fact]
        public async Task MakeOrder_Returns_BadRequest_When_Request_Null() // RED
        {
            var mock = new Mock<IOrderService>();
            var ctl = CreateControllerWithUser(mock.Object);

            var r = await ctl.MakeOrder(null!); // nếu hiện ném exception, test sẽ fail -> ép dev thêm null-check
            LogResult("MakeOrder_NullRequest", r);

            var bad = r.Should().BeOfType<BadRequestObjectResult>().Subject;
            bad.Value.Should().Be("Request body cannot be null.");

            mock.Verify(s => s.MakeOrder(It.IsAny<MakeOrderDto>(), It.IsAny<string>()), Times.Never);
        }
        [Fact]
        public async Task MakeOrder_Returns_BadRequest_When_No_UserId()
        {
            var mock = new Mock<IOrderService>();
            var ctl = CreateControllerWithUser(mock.Object, sellerId: null);

            var r = await ctl.MakeOrder(new MakeOrderDto { OrderCreate = new OrderCreateRequest() });
            LogResult("MakeOrder_NoUser", r);

            var bad = r.Should().BeOfType<BadRequestObjectResult>().Subject;
            bad.Value.Should().Be("SellerUserId not found.");

            mock.Verify(s => s.MakeOrder(It.IsAny<MakeOrderDto>(), It.IsAny<string>()), Times.Never);
        }
        [Fact]
        public async Task MakeOrder_Returns_BadRequest_When_Service_Task_Faulted()
        {
            var req = new MakeOrderDto { OrderCreate = new OrderCreateRequest() };
            var mock = new Mock<IOrderService>();
            mock.Setup(s => s.MakeOrder(req, "SELLER123"))
                .Returns(Task.FromException<MakeOrderResponse>(new Exception("task failed")));

            var ctl = CreateControllerWithUser(mock.Object);

            var r = await ctl.MakeOrder(req);
            LogResult("MakeOrder_TaskFaulted", r);

            var bad = r.Should().BeOfType<BadRequestObjectResult>().Subject;
            bad.Value.Should().Be("task failed");
        }
        //validate nội dung MakeOrderDto
        [Fact]
        public async Task MakeOrder_Returns_BadRequest_When_OrderCreate_Is_Missing() // RED
        {
            var mock = new Mock<IOrderService>();
            var ctl = CreateControllerWithUser(mock.Object);

            var r = await ctl.MakeOrder(new MakeOrderDto { OrderCreate = null! });
            LogResult("MakeOrder_OrderCreateMissing", r);

            var bad = r.Should().BeOfType<BadRequestObjectResult>().Subject;
            bad.Value.Should().Be("OrderCreate is required.");
            mock.Verify(s => s.MakeOrder(It.IsAny<MakeOrderDto>(), It.IsAny<string>()), Times.Never);
        }
        [Theory]
        [InlineData(1)]
        [InlineData(123)]
        public async Task MakeOrder_Returns_Ok_For_Various_Inputs(int endCustomerId)
        {
            var req = new MakeOrderDto { OrderCreate = new OrderCreateRequest { EndCustomerID = endCustomerId } };
            var mock = new Mock<IOrderService>();
            mock.Setup(s => s.MakeOrder(req, "SELLER123")).ReturnsAsync(new MakeOrderResponse());

            var ctl = CreateControllerWithUser(mock.Object);

            var r = await ctl.MakeOrder(req);
            LogResult($"MakeOrder_Varied_{endCustomerId}", r);

            var ok = r.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(new { message = "Order created susscessfully!" });
        }

    }
}

