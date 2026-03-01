using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Xunit;
using System.Reflection;

namespace Lumino.Tests;

public class LuminoSeederSunSceneTests
{
    [Fact]
    public void EnsureFinalSceneForTopic_WhenDialogSceneExists_ShouldReuseIt_AndMarkAsSun_WithoutDuplicate()
    {
        using var dbContext = TestDbContextFactory.Create();

        var course = new Course
        {
            Id = 1,
            Title = "English A1",
            Description = "Desc",
            IsPublished = true,
            LanguageCode = "en"
        };

        var topic = new Topic
        {
            Id = 10,
            CourseId = 1,
            Title = "Buy clothes",
            Order = 1
        };

        // This scene is seeded by SeedScenes() and initially not linked to the course/topic.
        var dialogScene = new Scene
        {
            Id = 100,
            CourseId = null,
            TopicId = null,
            Order = 1,
            Title = "Cafe order",
            Description = "Dialog scene",
            SceneType = "Dialog"
        };

        dbContext.Courses.Add(course);
        dbContext.Topics.Add(topic);
        dbContext.Scenes.Add(dialogScene);
        dbContext.SaveChanges();

        var method = typeof(LuminoSeeder).GetMethod("EnsureFinalSceneForTopic", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        method!.Invoke(null, new object[] { dbContext, course, topic });

        dbContext.SaveChanges();

        var sunScenes = dbContext.Scenes
            .Where(x => x.CourseId == course.Id && x.TopicId == topic.Id && x.SceneType == "Sun")
            .ToList();

        Assert.Single(sunScenes);

        var sun = sunScenes[0];
        Assert.Equal("Cafe order", sun.Title);
        Assert.Equal(1000 + topic.Order, sun.Order);

        // No other scenes should be linked to this topic/course by seeder (avoid duplicates).
        var allLinkedToTopic = dbContext.Scenes
            .Where(x => x.CourseId == course.Id && x.TopicId == topic.Id)
            .ToList();

        Assert.Single(allLinkedToTopic);
    }
}
