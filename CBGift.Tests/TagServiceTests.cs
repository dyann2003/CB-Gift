using AutoMapper;
using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Mapper;
using CB_Gift.Models;
using CB_Gift.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CB_Gift.Tests.Services
{
    public class TagServiceTests
    {
        private static CBGiftDbContext NewInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<CB_Gift.Data.CBGiftDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var ctx = new CBGiftDbContext(options);

            // Đảm bảo tạo DB + quan hệ nếu dùng Fluent API ở OnModelCreating
            ctx.Database.EnsureCreated();
            return ctx;
        }

        private static IMapper CreateMapper()
        {
            var cfg = new MapperConfiguration(c =>
            {
                c.AddProfile<MapperProfile>();
            });
            cfg.AssertConfigurationIsValid();
            return cfg.CreateMapper();
        }

        private static async Task SeedProductsAsync(CBGiftDbContext ctx)
        {
            // Seed một product trống tags
            ctx.Products.Add(new Product
            {
                ProductId = 1,
                ProductName = "P1",
                Tags = new List<Tag>()
            });

            ctx.Products.Add(new Product
            {
                ProductId = 2,
                ProductName = "P2",
                Tags = new List<Tag>()
            });

            await ctx.SaveChangesAsync();
        }

        [Fact]
        public async Task CreateTagAsync_Should_Create_New_Tag_When_Code_Not_Duplicated()
        {
            var ctx = NewInMemoryContext(nameof(CreateTagAsync_Should_Create_New_Tag_When_Code_Not_Duplicated));
            var mapper = CreateMapper();
            var service = new TagService(ctx, mapper);

            var dto = new CreateTagDto { TagCode = "SUMMER", TagName = "Summer" };

            var result = await service.CreateTagAsync(dto);

            Assert.NotNull(result);
            Assert.Equal("SUMMER", result.TagCode);
            Assert.True(result.TagsId > 0);

            var exists = await ctx.Tags.AnyAsync(t => t.TagCode == "SUMMER");
            Assert.True(exists);
        }

        [Fact]
        public async Task CreateTagAsync_Should_Throw_When_Code_Duplicated_Ignoring_Case()
        {
            var ctx = NewInMemoryContext(nameof(CreateTagAsync_Should_Throw_When_Code_Duplicated_Ignoring_Case));
            var mapper = CreateMapper();
            ctx.Tags.Add(new Tag { TagCode = "sale", TagName = "Sale" });
            await ctx.SaveChangesAsync();

            var service = new TagService(ctx, mapper);
            var dto = new CreateTagDto { TagCode = "SALE", TagName = "Sale 2" };

            await Assert.ThrowsAsync<Exception>(async () => await service.CreateTagAsync(dto));
        }

        [Fact]
        public async Task DeleteTagAsync_Should_Return_False_When_Not_Found()
        {
            var ctx = NewInMemoryContext(nameof(DeleteTagAsync_Should_Return_False_When_Not_Found));
            var mapper = CreateMapper();
            var service = new TagService(ctx, mapper);

            var ok = await service.DeleteTagAsync(999);
            Assert.False(ok);
        }

        [Fact]
        public async Task DeleteTagAsync_Should_Delete_And_Return_True()
        {
            var ctx = NewInMemoryContext(nameof(DeleteTagAsync_Should_Delete_And_Return_True));
            var mapper = CreateMapper();
            var tag = new Tag { TagCode = "DEL", TagName = "Delete Me" };
            ctx.Tags.Add(tag);
            await ctx.SaveChangesAsync();

            var service = new TagService(ctx, mapper);
            var ok = await service.DeleteTagAsync(tag.TagsId);

            Assert.True(ok);
            Assert.False(await ctx.Tags.AnyAsync(t => t.TagsId == tag.TagsId));
        }

        [Fact]
        public async Task GetAllTagsAsync_Should_Return_Mapped_Dtos()
        {
            var ctx = NewInMemoryContext(nameof(GetAllTagsAsync_Should_Return_Mapped_Dtos));
            var mapper = CreateMapper();
            ctx.Tags.AddRange(
                new Tag { TagCode = "T1", TagName = "Tag 1" },
                new Tag { TagCode = "T2", TagName = "Tag 2" }
            );
            await ctx.SaveChangesAsync();

            var service = new TagService(ctx, mapper);
            var list = (await service.GetAllTagsAsync()).ToList();

            Assert.Equal(2, list.Count);
            Assert.Contains(list, t => t.TagCode == "T1");
            Assert.Contains(list, t => t.TagCode == "T2");
        }

        [Fact]
        public async Task GetTagByIdAsync_Should_Return_Null_When_Not_Found()
        {
            var ctx = NewInMemoryContext(nameof(GetTagByIdAsync_Should_Return_Null_When_Not_Found));
            var mapper = CreateMapper();
            var service = new TagService(ctx, mapper);

            var result = await service.GetTagByIdAsync(12345);
            Assert.Null(result);
        }

        [Fact]
        public async Task GetTagByIdAsync_Should_Return_Mapped_Dto()
        {
            var ctx = NewInMemoryContext(nameof(GetTagByIdAsync_Should_Return_Mapped_Dto));
            var mapper = CreateMapper();
            var tag = new Tag { TagCode = "FIND", TagName = "Find Me" };
            ctx.Tags.Add(tag);
            await ctx.SaveChangesAsync();

            var service = new TagService(ctx, mapper);
            var dto = await service.GetTagByIdAsync(tag.TagsId);

            Assert.NotNull(dto);
            Assert.Equal(tag.TagsId, dto.TagsId);
            Assert.Equal("FIND", dto.TagCode);
        }

        [Fact]
        public async Task UpdateTagAsync_Should_Return_False_When_Not_Found()
        {
            var ctx = NewInMemoryContext(nameof(UpdateTagAsync_Should_Return_False_When_Not_Found));
            var mapper = CreateMapper();
            var service = new TagService(ctx, mapper);

            var ok = await service.UpdateTagAsync(999, new UpdateTagDto { TagName = "New Name" });
            Assert.False(ok);
        }

        [Fact]
        public async Task UpdateTagAsync_Should_Update_And_Return_True()
        {
            var ctx = NewInMemoryContext(nameof(UpdateTagAsync_Should_Update_And_Return_True));
            var mapper = CreateMapper();
            var tag = new Tag { TagCode = "UP", TagName = "Old" };
            ctx.Tags.Add(tag);
            await ctx.SaveChangesAsync();

            var service = new TagService(ctx, mapper);
            var ok = await service.UpdateTagAsync(tag.TagsId, new UpdateTagDto { TagName = "New" });

            Assert.True(ok);
            var reloaded = await ctx.Tags.FindAsync(tag.TagsId);
            Assert.Equal("New", reloaded.TagName);
        }

        [Fact]
        public async Task AddTagToProductAsync_Should_Return_False_When_Product_Or_Tag_Not_Found()
        {
            var ctx = NewInMemoryContext(nameof(AddTagToProductAsync_Should_Return_False_When_Product_Or_Tag_Not_Found));
            var mapper = CreateMapper();
            await SeedProductsAsync(ctx);

            var service = new TagService(ctx, mapper);

            // Product không tồn tại
            var r1 = await service.AddTagToProductAsync(999, 1);
            Assert.False(r1);

            // Tag không tồn tại
            var r2 = await service.AddTagToProductAsync(1, 999);
            Assert.False(r2);
        }

        [Fact]
        public async Task AddTagToProductAsync_Should_Add_Tag_When_Not_Exists()
        {
            var ctx = NewInMemoryContext(nameof(AddTagToProductAsync_Should_Add_Tag_When_Not_Exists));
            var mapper = CreateMapper();
            await SeedProductsAsync(ctx);

            var tag = new Tag { TagCode = "ATTACH", TagName = "Attach" };
            ctx.Tags.Add(tag);
            await ctx.SaveChangesAsync();

            var service = new TagService(ctx, mapper);
            var ok = await service.AddTagToProductAsync(1, tag.TagsId);

            Assert.True(ok);

            var product = await ctx.Products.Include(p => p.Tags).FirstAsync(p => p.ProductId == 1);
            Assert.Single(product.Tags);
            Assert.Equal(tag.TagsId, product.Tags.First().TagsId);
        }

        [Fact]
        public async Task AddTagToProductAsync_Should_Not_Duplicate_When_Already_Exists()
        {
            var ctx = NewInMemoryContext(nameof(AddTagToProductAsync_Should_Not_Duplicate_When_Already_Exists));
            var mapper = CreateMapper();
            await SeedProductsAsync(ctx);

            var tag = new Tag { TagCode = "DUP", TagName = "Dup" };
            ctx.Tags.Add(tag);
            await ctx.SaveChangesAsync();

            // Gắn lần 1
            ctx.Products.First(p => p.ProductId == 1).Tags.Add(tag);
            await ctx.SaveChangesAsync();

            var service = new TagService(ctx, mapper);
            // Gắn lần 2
            var ok = await service.AddTagToProductAsync(1, tag.TagsId);

            Assert.True(ok); // service vẫn trả true nhưng không thêm trùng
            var product = await ctx.Products.Include(p => p.Tags).FirstAsync(p => p.ProductId == 1);
            Assert.Single(product.Tags);
        }

        [Fact]
        public async Task RemoveTagFromProductAsync_Should_Return_False_When_Product_Not_Found()
        {
            var ctx = NewInMemoryContext(nameof(RemoveTagFromProductAsync_Should_Return_False_When_Product_Not_Found));
            var mapper = CreateMapper();
            var service = new TagService(ctx, mapper);

            var ok = await service.RemoveTagFromProductAsync(999, 1);
            Assert.False(ok);
        }

        [Fact]
        public async Task RemoveTagFromProductAsync_Should_Remove_Tag_If_Exists_And_Return_True()
        {
            var ctx = NewInMemoryContext(nameof(RemoveTagFromProductAsync_Should_Remove_Tag_If_Exists_And_Return_True));
            var mapper = CreateMapper();
            await SeedProductsAsync(ctx);

            var tag = new Tag { TagCode = "R", TagName = "Removable" };
            ctx.Tags.Add(tag);
            ctx.Products.First(p => p.ProductId == 1).Tags.Add(tag);
            await ctx.SaveChangesAsync();

            var service = new TagService(ctx, mapper);
            var ok = await service.RemoveTagFromProductAsync(1, tag.TagsId);

            Assert.True(ok);

            var product = await ctx.Products.Include(p => p.Tags).FirstAsync(p => p.ProductId == 1);
            Assert.Empty(product.Tags);
        }

        [Fact]
        public async Task RemoveTagFromProductAsync_Should_Return_True_When_Tag_Not_In_Product_But_Product_Exists()
        {
            var ctx = NewInMemoryContext(nameof(RemoveTagFromProductAsync_Should_Return_True_When_Tag_Not_In_Product_But_Product_Exists));
            var mapper = CreateMapper();
            await SeedProductsAsync(ctx);

            var tag = new Tag { TagCode = "X", TagName = "X" };
            ctx.Tags.Add(tag);
            await ctx.SaveChangesAsync();

            var service = new TagService(ctx, mapper);
            // Tag không có trong product => code hiện tại vẫn return true
            var ok = await service.RemoveTagFromProductAsync(1, tag.TagsId);

            Assert.True(ok);
            var product = await ctx.Products.Include(p => p.Tags).FirstAsync(p => p.ProductId == 1);
            Assert.Empty(product.Tags);
        }

        [Fact]
        public async Task GetTagsByProductIdAsync_Should_Return_Null_When_Product_Not_Found()
        {
            var ctx = NewInMemoryContext(nameof(GetTagsByProductIdAsync_Should_Return_Null_When_Product_Not_Found));
            var mapper = CreateMapper();
            var service = new TagService(ctx, mapper);

            var result = await service.GetTagsByProductIdAsync(12345);
            Assert.Null(result);
        }

        [Fact]
        public async Task GetTagsByProductIdAsync_Should_Return_Mapped_Tags_When_Product_Exists()
        {
            var ctx = NewInMemoryContext(nameof(GetTagsByProductIdAsync_Should_Return_Mapped_Tags_When_Product_Exists));
            var mapper = CreateMapper();
            await SeedProductsAsync(ctx);

            var t1 = new Tag { TagCode = "A", TagName = "A" };
            var t2 = new Tag { TagCode = "B", TagName = "B" };
            ctx.Tags.AddRange(t1, t2);
            ctx.Products.First(p => p.ProductId == 1).Tags.Add(t1);
            ctx.Products.First(p => p.ProductId == 1).Tags.Add(t2);
            await ctx.SaveChangesAsync();

            var service = new TagService(ctx, mapper);
            var tags = (await service.GetTagsByProductIdAsync(1)).ToList();

            Assert.Equal(2, tags.Count);
            Assert.Contains(tags, x => x.TagCode == "A");
            Assert.Contains(tags, x => x.TagCode == "B");
        }
    }
}
