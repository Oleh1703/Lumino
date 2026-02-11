namespace Lumino.Api.Application.DTOs
{
    public class ActiveCourseResponse
    {
        public int CourseId { get; set; }

        public DateTime StartedAt { get; set; }

        public int? LastLessonId { get; set; }

        public DateTime LastOpenedAt { get; set; }
    }
}
