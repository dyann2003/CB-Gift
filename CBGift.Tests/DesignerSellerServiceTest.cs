using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Models;
using CB_Gift.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CB_Gift.Tests.Services
{
    public class DesignerSellerServiceTests
    {
        // Tạo DbContext InMemory
        private static CBGiftDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<CBGiftDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .EnableSensitiveDataLogging()
                .Options;

            return new CBGiftDbContext(options);
        }

        private static IMapper CreateMapper()
        {
            var cfg = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<DesignerSeller, DesignerSellerDto>()   // KHÔNG dùng ForAllOtherMembers
                   .ForMember(d => d.DesignerUserId, m => m.MapFrom(s => s.DesignerUserId))
                   .ForMember(d => d.SellerUserId, m => m.MapFrom(s => s.SellerUserId))
                   .ForMember(d => d.CreatedAt, m => m.MapFrom(s => s.CreatedAt))
                   .ForMember(d => d.CreatedByUserId, m => m.MapFrom(s => s.CreatedByUserId))
                   .ForMember(d => d.DesignerName, m => m.MapFrom(s => s.DesignerUser != null ? s.DesignerUser.UserName : null))
                   .ForMember(d => d.SellerName, m => m.MapFrom(s => s.SellerUser != null ? s.SellerUser.UserName : null));
            });

            cfg.AssertConfigurationIsValid();
            return cfg.CreateMapper();
        }


        // Seed data về user
        private static async Task SeedUsersAsync(CBGiftDbContext ctx, string designerId = "designer-1", string sellerId = "seller-1")
        {
            ctx.Users.AddRange(
                new AppUser { Id = designerId, UserName = "designer.user" },
                new AppUser { Id = sellerId, UserName = "seller.user" }
            );
            await ctx.SaveChangesAsync();
        }

        [Fact]
        public async Task AssignDesignerToSellerAsync_CreatesNewAssignment_WhenNotExists()
        {
            var dbName = Guid.NewGuid().ToString();
            using var ctx = CreateDbContext(dbName);
            await SeedUsersAsync(ctx);
            var mapper = CreateMapper();
            var service = new DesignerSellerService(ctx, mapper);

            var dto = new AssignDesignerDto
            {
                DesignerUserId = "designer-1",
                SellerUserId = "seller-1"
            };

            var result = await service.AssignDesignerToSellerAsync(dto, "manager-99");

            Assert.True(result);
            var assignments = await ctx.DesignerSellers.ToListAsync();
            Assert.Single(assignments);
            Assert.Equal("designer-1", assignments[0].DesignerUserId);
            Assert.Equal("seller-1", assignments[0].SellerUserId);
            Assert.Equal("manager-99", assignments[0].CreatedByUserId);
        }

        [Fact]
        public async Task AssignDesignerToSellerAsync_DoesNotDuplicate_WhenExists()
        {
            var dbName = Guid.NewGuid().ToString();
            using var ctx = CreateDbContext(dbName);
            await SeedUsersAsync(ctx);
            var mapper = CreateMapper();
            var service = new DesignerSellerService(ctx, mapper);

            var dto = new AssignDesignerDto { DesignerUserId = "designer-1", SellerUserId = "seller-1" };

            ctx.DesignerSellers.Add(new DesignerSeller
            {
                DesignerUserId = "designer-1",
                SellerUserId = "seller-1",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = "seed"
            });
            await ctx.SaveChangesAsync();

            var result = await service.AssignDesignerToSellerAsync(dto, "manager-99");

            Assert.True(result);
            Assert.Equal(1, await ctx.DesignerSellers.CountAsync());
        }

        [Fact]
        public async Task RemoveDesignerFromSellerAsync_Removes_WhenExists()
        {
            var dbName = Guid.NewGuid().ToString();
            using var ctx = CreateDbContext(dbName);
            await SeedUsersAsync(ctx);
            var mapper = CreateMapper();
            var service = new DesignerSellerService(ctx, mapper);

            var dto = new AssignDesignerDto { DesignerUserId = "designer-1", SellerUserId = "seller-1" };

            ctx.DesignerSellers.Add(new DesignerSeller
            {
                DesignerUserId = "designer-1",
                SellerUserId = "seller-1",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = "seed"
            });
            await ctx.SaveChangesAsync();

            var removed = await service.RemoveDesignerFromSellerAsync(dto);

            Assert.True(removed);
            Assert.Empty(ctx.DesignerSellers);
        }

        [Fact]
        public async Task RemoveDesignerFromSellerAsync_ReturnsFalse_IfNotExist()
        {
            var dbName = Guid.NewGuid().ToString();
            using var ctx = CreateDbContext(dbName);
            var mapper = CreateMapper();
            var service = new DesignerSellerService(ctx, mapper);

            var dto = new AssignDesignerDto { DesignerUserId = "designer-1", SellerUserId = "seller-1" };

            var removed = await service.RemoveDesignerFromSellerAsync(dto);

            Assert.False(removed);
        }

        [Fact]
        public async Task GetDesignersForSellerAsync_ReturnsDesignerList()
        {
            var dbName = Guid.NewGuid().ToString();
            using var ctx = CreateDbContext(dbName);
            await SeedUsersAsync(ctx);

            ctx.DesignerSellers.Add(new DesignerSeller
            {
                DesignerUserId = "designer-1",
                SellerUserId = "seller-1",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = "manager"
            });
            await ctx.SaveChangesAsync();

            var mapper = CreateMapper();
            var service = new DesignerSellerService(ctx, mapper);

            var result = (await service.GetDesignersForSellerAsync("seller-1")).ToList();

            Assert.Single(result);
            Assert.Equal("designer-1", result[0].DesignerUserId);
        }

        [Fact]
        public async Task GetSellersForDesignerAsync_ReturnsSellerList()
        {
            var dbName = Guid.NewGuid().ToString();
            using var ctx = CreateDbContext(dbName);
            await SeedUsersAsync(ctx);

            ctx.DesignerSellers.Add(new DesignerSeller
            {
                DesignerUserId = "designer-1",
                SellerUserId = "seller-1",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = "manager"
            });
            await ctx.SaveChangesAsync();

            var mapper = CreateMapper();
            var service = new DesignerSellerService(ctx, mapper);

            var result = (await service.GetSellersForDesignerAsync("designer-1")).ToList();

            Assert.Single(result);
            Assert.Equal("seller-1", result[0].SellerUserId);
        }
    }
}
