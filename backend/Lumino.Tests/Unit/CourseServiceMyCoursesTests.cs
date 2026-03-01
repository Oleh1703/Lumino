using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Xunit;

namespace Lumino.Tests;

public class CourseServiceMyCoursesTests
{
    [Fact]
    public void GetMyCourses_UsesExplicitOrderAndPrerequisite()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Courses.AddRange(
            new Course { Id = 1, Title = "English A1", Description = "D1", IsPublished = true, Level = "A1", Order = 1, PrerequisiteCourseId = null },
            new Course { Id = 2, Title = "English A2", Description = "D2", IsPublished = true, Level = "A2", Order = 2, PrerequisiteCourseId = 1 },
            new Course { Id = 3, Title = "English B1", Description = "D3", IsPublished = true, Level = "B1", Order = 3, PrerequisiteCourseId = 2 }
        );

        dbContext.SaveChanges();

        var completionService = new DictCourseCompletionService(new Dictionary<int, bool>
        {
            [1] = true,
            [2] = false,
            [3] = false
        });

        var service = new CourseService(dbContext, completionService);

        var result = service.GetMyCourses(userId: 1);

        Assert.Equal(3, result.Count);

        Assert.Equal(1, result[0].Id);
        Assert.False(result[0].IsLocked);

        Assert.Equal(2, result[1].Id);
        Assert.False(result[1].IsLocked);

        Assert.Equal(3, result[2].Id);
        Assert.True(result[2].IsLocked);
    }

    private class DictCourseCompletionService : ICourseCompletionService
    {
        private readonly Dictionary<int, bool> _completedMap;

        public DictCourseCompletionService(Dictionary<int, bool> completedMap)
        {
            _completedMap = completedMap;
        }

        public CourseCompletionResponse GetMyCourseCompletion(int userId, int courseId)
        {
            var isCompleted = _completedMap.TryGetValue(courseId, out var completed) && completed;

            return new CourseCompletionResponse
            {
                CourseId = courseId,
                Status = isCompleted ? "Completed" : "NotStarted",
                IsCompleted = isCompleted,
                CompletedAt = isCompleted ? DateTime.UtcNow : null,
                TotalLessons = 0,
                CompletedLessons = 0,
                CompletionPercent = isCompleted ? 100 : 0,
                NextLessonId = null,
                RemainingLessonIds = new List<int>(),
                ScenesIncluded = false,
                ScenesTotal = 0,
                ScenesCompleted = 0,
                ScenesCompletionPercent = 0
            };
        }
    }
}
