using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CB_Gift.Data;

namespace CB_Gift.Tests.Utils
{
    public static class InMemoryDbFactory
    {
        public static CBGiftDbContext CreateContext(string? dbName = null)
        {
            dbName ??= $"CBGift_Tests_{Guid.NewGuid()}";

            var options = new DbContextOptionsBuilder<CBGiftDbContext>()
                .UseInMemoryDatabase(dbName)
             
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .EnableSensitiveDataLogging()
                .Options;

            return new CBGiftDbContext(options);
        }
    }
}
