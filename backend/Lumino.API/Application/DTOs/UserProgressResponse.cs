namespace Lumino.Api.Application.DTOs
{
    public class UserProgressResponse
    {
        public int CompletedLessons { get; set; }

        public int TotalScore { get; set; }

        public DateTime LastUpdatedAt { get; set; }

        public int TotalLessons { get; set; }

        public int CompletedDistinctLessons { get; set; }

        public int CompletionPercent { get; set; }

        public int CurrentStreakDays { get; set; }

        public DateTime? LastStudyAt { get; set; }

        public int TotalScenes { get; set; }

        public int CompletedDistinctScenes { get; set; }

        public int TotalVocabulary { get; set; }

        public int DueVocabulary { get; set; }

        public DateTime? NextVocabularyReviewAt { get; set; }
    }
}
