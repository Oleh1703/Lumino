using Lumino.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Lumino.Tests;

public static class TestDbContextFactory
{
    public static LuminoDbContext Create()
    {
        var options = new DbContextOptionsBuilder<LuminoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new LuminoDbContext(options);
        dbContext.Database.EnsureCreated();

        return dbContext;
    }
}
