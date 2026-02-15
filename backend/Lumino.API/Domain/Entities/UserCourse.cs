namespace Lumino.Api.Domain.Entities
{
    public class UserCourse
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int CourseId { get; set; }

        public bool IsActive { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime? CompletedAt { get; set; }

        public int? LastLessonId { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime LastOpenedAt { get; set; }
    }
}
