// Tests/Utils/InMemoryDbFactory.cs
using System;
using CB_Gift.Data;
using Microsoft.EntityFrameworkCore;

namespace CB_Gift.Tests.Utils
{
    public static class InMemoryDbFactory
    {
        public static CBGiftDbContext CreateContext(string? dbName = null)
        {
            dbName ??= $"CBGift_Tests_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<CBGiftDbContext>()
                .UseInMemoryDatabase(dbName)
                .EnableSensitiveDataLogging()
                .Options;

            var ctx = new CBGiftDbContext(options);
            return ctx;
        }
    }
}
