using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;

namespace Lumino.Tests;

public class FakeCourseCompletionService : ICourseCompletionService
{
    public CourseCompletionResponse GetMyCourseCompletion(int userId, int courseId)
    {
        return new CourseCompletionResponse
        {
            CourseId = courseId,
            Status = "NotStarted",
            IsCompleted = false,
            CompletedAt = null,
            TotalLessons = 0,
            CompletedLessons = 0,
            CompletionPercent = 0,
            NextLessonId = null,
            RemainingLessonIds = new List<int>(),
            ScenesIncluded = false,
            ScenesTotal = 0,
            ScenesCompleted = 0,
            ScenesCompletionPercent = 0
        };
    }
}
