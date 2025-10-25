using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Models;
using CB_Gift.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace CBGift.Tests
{
    public class CategoryServiceTests
    {
        private static CBGiftDbContext BuildDb(string dbName)
        {
            var options = new DbContextOptionsBuilder<CBGiftDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var ctx = new CBGiftDbContext(options);
            ctx.Database.EnsureCreated();
            return ctx;
        }

        private static IMapper BuildMapper()
        {
            var cfg = new MapperConfiguration(c =>
            {
                c.CreateMap<Category, CategoryDto>();
                c.CreateMap<CreateCategoryDto, Category>();
                c.CreateMap<UpdateCategoryDto, Category>();
            });
            return cfg.CreateMapper();
        }

        private static async Task SeedAsync(CBGiftDbContext ctx)
        {
            ctx.Categories.RemoveRange(ctx.Categories);
            ctx.Products.RemoveRange(ctx.Products);

            var cats = new[]
            {
                new Category { CategoryId = 1, CategoryCode = "CAT01", CategoryName = "Cat A", Status = 1 },
                new Category { CategoryId = 2, CategoryCode = "CAT02", CategoryName = "Cat B", Status = 0 },
                new Category { CategoryId = 3, CategoryCode = "CAT03", CategoryName = "Cat C", Status = 1 },
            };
            await ctx.Categories.AddRangeAsync(cats);

            // Một sản phẩm gắn với CategoryId=2 để test ràng buộc xóa
            await ctx.Products.AddAsync(new Product { ProductId = 10, ProductName = "P1", CategoryId = 2 });

            await ctx.SaveChangesAsync();
        }

        [Fact]
        public async Task GetAllCategoriesAsync_ReturnsAll()
        {
            var db = BuildDb(nameof(GetAllCategoriesAsync_ReturnsAll));
            await SeedAsync(db);
            var svc = new CategoryService(db, BuildMapper());

            var result = await svc.GetAllCategoriesAsync();

            result.Should().HaveCount(3);
            result.Select(x => x.CategoryCode).Should().BeEquivalentTo(new[] { "CAT01", "CAT02", "CAT03" });
        }

        [Fact]
        public async Task GetCategoryByIdAsync_ReturnsOne_WhenExists()
        {
            var db = BuildDb(nameof(GetCategoryByIdAsync_ReturnsOne_WhenExists));
            await SeedAsync(db);
            var svc = new CategoryService(db, BuildMapper());

            var dto = await svc.GetCategoryByIdAsync(1);

            dto.Should().NotBeNull();
            dto!.CategoryId.Should().Be(1);
            dto.CategoryCode.Should().Be("CAT01");
        }

        [Fact]
        public async Task GetCategoryByIdAsync_ReturnsNull_WhenNotExists()
        {
            var db = BuildDb(nameof(GetCategoryByIdAsync_ReturnsNull_WhenNotExists));
            await SeedAsync(db);
            var svc = new CategoryService(db, BuildMapper());

            var dto = await svc.GetCategoryByIdAsync(999);

            dto.Should().BeNull();
        }

        [Fact]
        public async Task GetActiveCategoriesAsync_ReturnsOnlyStatus1()
        {
            var db = BuildDb(nameof(GetActiveCategoriesAsync_ReturnsOnlyStatus1));
            await SeedAsync(db);
            var svc = new CategoryService(db, BuildMapper());

            var result = await svc.GetActiveCategoriesAsync();

            result.Should().HaveCount(2);
            result.All(x => x.Status == 1).Should().BeTrue();
        }

        [Fact]
        public async Task CreateCategoryAsync_Creates_WhenUniqueCode()
        {
            var db = BuildDb(nameof(CreateCategoryAsync_Creates_WhenUniqueCode));
            await SeedAsync(db);
            var svc = new CategoryService(db, BuildMapper());

            var create = new CreateCategoryDto
            {
                CategoryCode = "NEW01",
                CategoryName = "New Cat",
                Status = 1
            };

            var created = await svc.CreateCategoryAsync(create);

            created.Should().NotBeNull();
            created.CategoryId.Should().BeGreaterThan(0);
            created.CategoryCode.Should().Be("NEW01");
            (await db.Categories.CountAsync()).Should().Be(4);
        }

        [Fact]
        public async Task CreateCategoryAsync_Throws_WhenDuplicateCode_CaseInsensitive()
        {
            var db = BuildDb(nameof(CreateCategoryAsync_Throws_WhenDuplicateCode_CaseInsensitive));
            await SeedAsync(db);
            var svc = new CategoryService(db, BuildMapper());

            var dup = new CreateCategoryDto
            {
                CategoryCode = "cat01", // khác hoa/thường
                CategoryName = "Dup",
                Status = 1
            };

            var act = async () => await svc.CreateCategoryAsync(dup);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Mã danh mục này đã tồn tại.");
        }

        [Fact]
        public async Task UpdateCategoryAsync_ReturnsFalse_WhenNotFound()
        {
            var db = BuildDb(nameof(UpdateCategoryAsync_ReturnsFalse_WhenNotFound));
            await SeedAsync(db);
            var svc = new CategoryService(db, BuildMapper());

            var ok = await svc.UpdateCategoryAsync(999, new UpdateCategoryDto
            {
                CategoryName = "X",
                Status = 0
            });

            ok.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateCategoryAsync_Updates_WhenFound()
        {
            var db = BuildDb(nameof(UpdateCategoryAsync_Updates_WhenFound));
            await SeedAsync(db);
            var svc = new CategoryService(db, BuildMapper());

            var ok = await svc.UpdateCategoryAsync(1, new UpdateCategoryDto
            {
                CategoryName = "Cat A (Updated)",
                Status = 0
            });

            ok.Should().BeTrue();

            var entity = await db.Categories.FindAsync(1);
            entity!.CategoryName.Should().Be("Cat A (Updated)");
            entity.Status.Should().Be(0);
        }

        [Fact]
        public async Task UpdateCategoryStatusAsync_ReturnsFalse_WhenNotFound()
        {
            var db = BuildDb(nameof(UpdateCategoryStatusAsync_ReturnsFalse_WhenNotFound));
            await SeedAsync(db);
            var svc = new CategoryService(db, BuildMapper());

            var ok = await svc.UpdateCategoryStatusAsync(999, new UpdateCategoryStatusDto { Status = 1 });

            ok.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateCategoryStatusAsync_UpdatesStatus_WhenFound()
        {
            var db = BuildDb(nameof(UpdateCategoryStatusAsync_UpdatesStatus_WhenFound));
            await SeedAsync(db);
            var svc = new CategoryService(db, BuildMapper());

            var ok = await svc.UpdateCategoryStatusAsync(2, new UpdateCategoryStatusDto { Status = 1 });

            ok.Should().BeTrue();
            (await db.Categories.FindAsync(2))!.Status.Should().Be(1);
        }

        [Fact]
        public async Task DeleteCategoryAsync_ReturnsFalse_WhenNotFound()
        {
            var db = BuildDb(nameof(DeleteCategoryAsync_ReturnsFalse_WhenNotFound));
            await SeedAsync(db);
            var svc = new CategoryService(db, BuildMapper());

            var ok = await svc.DeleteCategoryAsync(999);

            ok.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteCategoryAsync_Throws_WhenHasProducts()
        {
            var db = BuildDb(nameof(DeleteCategoryAsync_Throws_WhenHasProducts));
            await SeedAsync(db);
            var svc = new CategoryService(db, BuildMapper());

            var act = async () => await svc.DeleteCategoryAsync(2); // có Product liên kết

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Không thể xóa danh mục đã có sản phẩm.");
        }

        [Fact]
        public async Task DeleteCategoryAsync_Deletes_WhenNoProducts()
        {
            var db = BuildDb(nameof(DeleteCategoryAsync_Deletes_WhenNoProducts));
            await SeedAsync(db);

            // đảm bảo CategoryId=3 không có product liên kết
            var svc = new CategoryService(db, BuildMapper());

            var ok = await svc.DeleteCategoryAsync(3);

            ok.Should().BeTrue();
            (await db.Categories.AnyAsync(c => c.CategoryId == 3)).Should().BeFalse();
        }
    }
}
