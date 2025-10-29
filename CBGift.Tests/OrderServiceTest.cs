﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Models;
using CB_Gift.Models.Enums;
using CB_Gift.Services;
using CB_Gift.Tests.Utils;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CB_Gift.Tests.Services
{
    public class OrderServiceTests
    {
        private readonly CBGiftDbContext _db;
        private readonly Mock<IMapper> _mapper;
        private readonly ILogger<OrderService> _logger;
        private readonly OrderService _svc;

        public OrderServiceTests()
        {
            _db = InMemoryDbFactory.CreateContext();
            _mapper = new Mock<IMapper>(MockBehavior.Strict);
            _logger = Mock.Of<ILogger<OrderService>>();
            _svc = new OrderService(_db, _mapper.Object, _logger);

            Seed(_db);
        }

        private static void Seed(CBGiftDbContext db)
        {
            var p = new Product { ProductId = 1, ProductName = "Mug" };
            var v1 = new ProductVariant { ProductVariantId = 10, ProductId = 1, BaseCost = 5m, ShipCost = 2m, ExtraShipping = 1m };
            var v2 = new ProductVariant { ProductVariantId = 11, ProductId = 1, BaseCost = 7m, ShipCost = 3m, ExtraShipping = 2m };

            var c = new EndCustomer { CustId = 1, Name = "John", Email = "john@x.com" };

            var o1 = new Order
            {
                OrderId = 100,
                SellerUserId = "sellerA",
                EndCustomerId = 1,
                OrderDate = DateTime.UtcNow.AddDays(-1),
                StatusOrder = 1,
                TotalCost = 0
            };
            var o2 = new Order
            {
                OrderId = 101,
                SellerUserId = "sellerB",
                EndCustomerId = 1,
                OrderDate = DateTime.UtcNow,
                StatusOrder = 1,
                TotalCost = 0
            };

            var od1 = new OrderDetail { OrderDetailId = 1000, OrderId = 100, ProductVariantId = 10, Quantity = 1, NeedDesign = true, ProductionStatus = ProductionStatus.CHECK_DESIGN };
            var od2 = new OrderDetail { OrderDetailId = 1001, OrderId = 100, ProductVariantId = 11, Quantity = 2, NeedDesign = true, ProductionStatus = ProductionStatus.CHECK_DESIGN };
            var od3 = new OrderDetail { OrderDetailId = 1002, OrderId = 101, ProductVariantId = 10, Quantity = 1, NeedDesign = false };

            db.Products.Add(p);
            db.ProductVariants.AddRange(v1, v2);
            db.EndCustomers.Add(c);
            db.Orders.AddRange(o1, o2);
            db.OrderDetails.AddRange(od1, od2, od3);
            db.SaveChanges();
        }

        //[Fact]
        //public async Task GetOrdersForSellerAsync_FiltersAndSorts()
        //{
        //    var list = await _svc.GetOrdersForSellerAsync("sellerA");

        //    list.Should().NotBeNull();
        //    list.Should().HaveCount(1);
        //    list.Select(x => x.OrderId).Should().Contain(100);
        //    list[0].CustomerName.Should().Be("John");
        //}

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

        [Fact]
        public async Task CreateOrderAsync_Maps_Saves_Defaults_And_TTS_Surcharge()
        {
            var req = new OrderCreateRequest { ActiveTTS = true };

            _mapper.Setup(m => m.Map<Order>(It.IsAny<OrderCreateRequest>()))
                   .Returns((OrderCreateRequest r) => new Order { ActiveTts = r.ActiveTTS, TotalCost = 0 });

            var id = await _svc.CreateOrderAsync(req, "sellerZ");

            id.Should().BeGreaterThan(0);
            var inDb = await _db.Orders.FindAsync(id);
            inDb.Should().NotBeNull();
            inDb!.SellerUserId.Should().Be("sellerZ");
            inDb.ProductionStatus.Should().Be("CREATED");
            inDb.StatusOrder.Should().Be(1);
            inDb.TotalCost.Should().Be(1);
        }

        [Fact]
        public async Task AddOrderDetailAsync_Fails_When_NotOwner()
        {
            var req = new OrderDetailCreateRequest { ProductVariantID = 10, Quantity = 1 };
            _mapper.Setup(m => m.Map<OrderDetail>(It.IsAny<OrderDetailCreateRequest>()))
                   .Returns((OrderDetailCreateRequest r) => new OrderDetail { ProductVariantId = r.ProductVariantID, Quantity = r.Quantity });

            var act = async () => await _svc.AddOrderDetailAsync(101, req, "sellerA");
            await act.Should().ThrowAsync<Exception>().WithMessage("*not yours*");
        }

        [Fact]
        public async Task AddOrderDetailAsync_Fails_When_Variant_NotFound()
        {
            var req = new OrderDetailCreateRequest { ProductVariantID = 999, Quantity = 1 };
            _mapper.Setup(m => m.Map<OrderDetail>(It.IsAny<OrderDetailCreateRequest>())).Returns(new OrderDetail());

            var act = async () => await _svc.AddOrderDetailAsync(100, req, "sellerA");
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*does not exist*");
        }

        [Fact]
        public async Task AddOrderDetailAsync_Fails_When_Quantity_Invalid()
        {
            var req = new OrderDetailCreateRequest { ProductVariantID = 10, Quantity = 0 };
            _mapper.Setup(m => m.Map<OrderDetail>(It.IsAny<OrderDetailCreateRequest>())).Returns(new OrderDetail());

            var act = async () => await _svc.AddOrderDetailAsync(100, req, "sellerA");
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Quantity must be greater than zero*");
        }

        [Fact]
        public async Task AddOrderDetailAsync_Adds_And_Recalculates_Total()
        {
            var req = new OrderDetailCreateRequest { ProductVariantID = 10, Quantity = 3 };
            _mapper.Setup(m => m.Map<OrderDetail>(It.IsAny<OrderDetailCreateRequest>()))
                   .Returns((OrderDetailCreateRequest r) => new OrderDetail { ProductVariantId = r.ProductVariantID, Quantity = r.Quantity });

            await _svc.AddOrderDetailAsync(100, req, "sellerA");

            var order = await _db.Orders.FindAsync(100);
            order!.TotalCost.Should().Be(47); // theo giải thích trong comment test trước
        }

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
            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*Daft(Nháp)*");
        }

        [Fact]
        public async Task DeleteOrderAsync_Removes_Order_And_Details_When_Draft()
        {
            var ok = await _svc.DeleteOrderAsync(100, "sellerA");
            ok.Should().BeTrue();

            (await _db.Orders.FindAsync(100)).Should().BeNull();
            (await _db.OrderDetails.Where(d => d.OrderId == 100).ToListAsync()).Should().BeEmpty();
        }

        [Fact]
        public async Task SellerApproveOrderDesignAsync_Sets_All_READY_PROD()
        {
            var o = await _db.Orders.Include(x => x.OrderDetails).FirstAsync(x => x.OrderId == 100);
            o.StatusOrder = 5;
            await _db.SaveChangesAsync();

            var ok = await _svc.SellerApproveOrderDesignAsync(100, ProductionStatus.READY_PROD, "sellerA");
            ok.Should().BeTrue();

            var updated = await _db.Orders.Include(x => x.OrderDetails).FirstAsync(x => x.OrderId == 100);
            updated.StatusOrder.Should().Be(7);
            updated.OrderDetails.Should().OnlyContain(d => d.ProductionStatus == ProductionStatus.READY_PROD);
        }

        [Fact]
        public async Task SellerApproveOrderDesignAsync_Sets_All_DESIGN_REDO()
        {
            var o = await _db.Orders.Include(x => x.OrderDetails).FirstAsync(x => x.OrderId == 100);
            o.StatusOrder = 5;
            foreach (var d in o.OrderDetails) d.ProductionStatus = ProductionStatus.CHECK_DESIGN;
            await _db.SaveChangesAsync();

            var ok = await _svc.SellerApproveOrderDesignAsync(100, ProductionStatus.DESIGN_REDO, "sellerA");
            ok.Should().BeTrue();

            var updated = await _db.Orders.Include(x => x.OrderDetails).FirstAsync(x => x.OrderId == 100);
            updated.StatusOrder.Should().Be(6);
            updated.OrderDetails.Should().OnlyContain(d => d.ProductionStatus == ProductionStatus.DESIGN_REDO);
        }

        [Fact]
        public async Task SellerApproveOrderDetailDesignAsync_Updates_Single_Detail()
        {
            var ok = await _svc.SellerApproveOrderDetailDesignAsync(1000, ProductionStatus.READY_PROD, "sellerA");
            ok.Should().BeTrue();

            (await _db.OrderDetails.FindAsync(1000))!.ProductionStatus.Should().Be(ProductionStatus.READY_PROD);
            (await _db.OrderDetails.FindAsync(1001))!.ProductionStatus.Should().Be(ProductionStatus.CHECK_DESIGN);
        }

        //[Fact]
        //public async Task SendOrderToReadyProdAsync_Moves_Order101_And_All_Details()
        //{
        //    var ok = await _svc.SendOrderToReadyProdAsync(101, "sellerB");
        //    ok.Should().BeTrue();

        //    var updated = await _db.Orders.Include(o => o.OrderDetails).FirstAsync(o => o.OrderId == 101);
        //    updated.StatusOrder.Should().Be(7);
        //    updated.OrderDetails.Should().OnlyContain(d => d.ProductionStatus == ProductionStatus.READY_PROD);
        //}

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
            var o = new Order { OrderId = 202, SellerUserId = "a", StatusOrder = 1 };
            _db.Orders.Add(o);
            await _db.SaveChangesAsync();

            var res = await _svc.ApproveOrderForShippingAsync(202);
            res.IsSuccess.Should().BeFalse();
            res.CanApprove.Should().BeFalse();
            res.ErrorMessage.Should().MatchRegex("(?i)no product details");


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
        public async Task ApproveOrderForShippingAsync_Succeeds_When_All_QC_Done()
        {
            var o = await _db.Orders.Include(x => x.OrderDetails).FirstAsync(x => x.OrderId == 100);
            foreach (var d in o.OrderDetails) d.ProductionStatus = ProductionStatus.QC_DONE;
            await _db.SaveChangesAsync();

            var res = await _svc.ApproveOrderForShippingAsync(100);
            res.IsSuccess.Should().BeTrue();

            (await _db.Orders.FindAsync(100))!.StatusOrder.Should().Be(14);
        }
    }
}
