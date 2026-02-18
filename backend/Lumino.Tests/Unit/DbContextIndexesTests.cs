using Lumino.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lumino.Tests;

public class DbContextIndexesTests
{
    [Fact]
    public void LessonResult_ShouldHaveUniqueIndex_OnUserIdAndIdempotencyKey()
    {
        var dbContext = TestDbContextFactory.Create();

        var entity = dbContext.Model.FindEntityType(typeof(LessonResult));
        Assert.NotNull(entity);

        var index = entity!.GetIndexes()
            .FirstOrDefault(x =>
                x.Properties.Count == 2
                && x.Properties.Any(p => p.Name == nameof(LessonResult.UserId))
                && x.Properties.Any(p => p.Name == nameof(LessonResult.IdempotencyKey))
            );

        Assert.NotNull(index);
        Assert.True(index!.IsUnique);
    }
}
