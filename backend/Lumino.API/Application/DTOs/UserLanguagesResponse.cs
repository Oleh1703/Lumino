namespace Lumino.Api.Application.DTOs
{
    public class UserLanguagesResponse
    {
        public string? NativeLanguageCode { get; set; }

        public string? ActiveTargetLanguageCode { get; set; }

        public List<UserLearningLanguageResponse> LearningLanguages { get; set; } = new();
    }

    public class UserLearningLanguageResponse
    {
        public string Code { get; set; } = null!;

        public string Title { get; set; } = null!;

        public bool IsActive { get; set; }
    }
}
