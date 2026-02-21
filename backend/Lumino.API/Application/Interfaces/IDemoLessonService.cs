using Lumino.Api.Application.DTOs;
using System.Collections.Generic;

namespace Lumino.Api.Application.Interfaces
{
    public interface IDemoLessonService
    {
        List<LessonResponse> GetDemoLessons();

        DemoNextLessonResponse GetDemoNextLesson(int step);

        DemoNextLessonPackResponse GetDemoNextLessonPack(int step);

        LessonResponse GetDemoLessonById(int lessonId);

        List<ExerciseResponse> GetDemoExercisesByLesson(int lessonId);

        SubmitLessonResponse SubmitDemoLesson(SubmitLessonRequest request);
    }
}
