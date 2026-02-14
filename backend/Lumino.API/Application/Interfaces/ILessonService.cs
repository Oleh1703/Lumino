using Lumino.Api.Application.DTOs;
using System.Collections.Generic;

namespace Lumino.Api.Application.Interfaces
{
    public interface ILessonService
    {
        List<LessonResponse> GetLessonsByTopic(int topicId);

        LessonResponse GetLessonById(int userId, int lessonId);
    }
}
