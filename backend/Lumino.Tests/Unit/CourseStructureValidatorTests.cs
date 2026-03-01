using Lumino.Api.Application.Validators;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Xunit;

namespace Lumino.Tests;

public class CourseStructureValidatorTests
{
    [Fact]
    public void ValidateOrThrow_ShouldPass_WhenStructureIsStrict()
    {
        var dbContext = TestDbContextFactory.Create();

        var course = new Course
        {
            Title = "English A1",
            Description = "Test",
            LanguageCode = "en",
            IsPublished = false
        };

        dbContext.Courses.Add(course);
        dbContext.SaveChanges();

        for (int t = 1; t <= 10; t++)
        {
            var topic = new Topic
            {
                CourseId = course.Id,
                Title = $"Topic {t}",
                Order = t
            };

            dbContext.Topics.Add(topic);
            dbContext.SaveChanges();

            for (int l = 1; l <= 8; l++)
            {
                var lesson = new Lesson
                {
                    TopicId = topic.Id,
                    Title = $"Lesson {l}",
                    Theory = "Theory",
                    Order = l
                };

                dbContext.Lessons.Add(lesson);
                dbContext.SaveChanges();

                for (int e = 1; e <= 9; e++)
                {
                    dbContext.Exercises.Add(new Exercise
                    {
                        LessonId = lesson.Id,
                        Type = ExerciseType.MultipleChoice,
                        Question = "Q",
                        Data = "[]",
                        CorrectAnswer = "A",
                        Order = e
                    });
                }

                dbContext.SaveChanges();
            }

            dbContext.Scenes.Add(new Scene
            {
                CourseId = course.Id,
                TopicId = topic.Id,
                Order = 10000 + t,
                Title = "Final",
                Description = "Final",
                SceneType = "Sun"
            });

            dbContext.SaveChanges();
        }

        var validator = new CourseStructureValidator(dbContext);

        validator.ValidateOrThrow(course.Id);
    }

    [Fact]
    public void ValidateOrThrow_ShouldThrow_WhenTopicsCountIsWrong()
    {
        var dbContext = TestDbContextFactory.Create();

        var course = new Course
        {
            Title = "English A1",
            Description = "Test",
            LanguageCode = "en",
            IsPublished = false
        };

        dbContext.Courses.Add(course);
        dbContext.SaveChanges();

        dbContext.Topics.Add(new Topic
        {
            CourseId = course.Id,
            Title = "Topic 1",
            Order = 1
        });

        dbContext.SaveChanges();

        var validator = new CourseStructureValidator(dbContext);

        var ex = Assert.Throws<ArgumentException>(() => validator.ValidateOrThrow(course.Id));
        Assert.Contains("exactly 10 topics", ex.Message);
    

    }

    [Fact]
    public void ValidateOrThrow_ShouldThrow_WhenTopicOrdersAreNotStrict()
    {
        var dbContext = TestDbContextFactory.Create();

        var course = new Course
        {
            Title = "English A1",
            Description = "Test",
            LanguageCode = "en",
            IsPublished = false
        };

        dbContext.Courses.Add(course);
        dbContext.SaveChanges();

        // 10 topics but wrong orders (duplicate 1, missing 2)
        for (int t = 1; t <= 10; t++)
        {
            dbContext.Topics.Add(new Topic
            {
                CourseId = course.Id,
                Title = $"Topic {t}",
                Order = t == 2 ? 1 : t
            });
        }

        dbContext.SaveChanges();

        var validator = new CourseStructureValidator(dbContext);

        var ex = Assert.Throws<ArgumentException>(() => validator.ValidateOrThrow(course.Id));
        Assert.Contains("topics order must be unique", ex.Message);
    }

}
