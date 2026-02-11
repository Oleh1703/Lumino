namespace Lumino.Api.Application.DTOs
{
    public class LearningPathResponse
    {
        public int CourseId { get; set; }

        public string CourseTitle { get; set; } = null!;

        public List<LearningPathTopicResponse> Topics { get; set; } = new();
    }

    public class LearningPathTopicResponse
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public int Order { get; set; }

        public List<LearningPathLessonResponse> Lessons { get; set; } = new();
    }

    public class LearningPathLessonResponse
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public int Order { get; set; }

        public bool IsUnlocked { get; set; }

        public bool IsPassed { get; set; }

        public int? BestScore { get; set; }

        public int? TotalQuestions { get; set; }

        public int? BestPercent { get; set; }
    }
}
