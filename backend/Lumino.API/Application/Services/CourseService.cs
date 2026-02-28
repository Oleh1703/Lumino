using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;

namespace Lumino.Api.Application.Services
{
    public class CourseService : ICourseService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly ICourseCompletionService _courseCompletionService;

        public CourseService(LuminoDbContext dbContext, ICourseCompletionService courseCompletionService)
        {
            _dbContext = dbContext;
            _courseCompletionService = courseCompletionService;
        }

        public List<CourseResponse> GetPublishedCourses(string? languageCode = null)
        {
            var query = _dbContext.Courses
                .Where(x => x.IsPublished);

            if (!string.IsNullOrWhiteSpace(languageCode))
            {
                if (!Lumino.Api.Utils.SupportedLanguages.IsLearnable(languageCode))
                {
                    throw new ArgumentException("LanguageCode is not supported");
                }

                var normalized = languageCode.Trim().ToLowerInvariant();
                query = query.Where(x => x.LanguageCode == normalized);
            }

            return query
                .Select(x => new CourseResponse
                {
                    Id = x.Id,
                    Title = x.Title,
                    Description = x.Description,
                    LanguageCode = x.LanguageCode
                })
                .ToList();
        }


        public List<CourseForMeResponse> GetMyCourses(int userId, string? languageCode = null)
        {
            var query = _dbContext.Courses
                .Where(x => x.IsPublished);

            if (!string.IsNullOrWhiteSpace(languageCode))
            {
                if (!Lumino.Api.Utils.SupportedLanguages.IsLearnable(languageCode))
                {
                    throw new ArgumentException("LanguageCode is not supported");
                }

                var normalized = languageCode.Trim().ToLowerInvariant();
                query = query.Where(x => x.LanguageCode == normalized);
            }

            var courses = query
                .AsEnumerable()
                .OrderBy(x => GetCourseLevelOrder(x.Title))
                .ThenBy(x => x.Id)
                .ToList();

            var result = new List<CourseForMeResponse>();

            bool previousCompleted = true;

            foreach (var c in courses)
            {
                var completion = _courseCompletionService.GetMyCourseCompletion(userId, c.Id);

                var isLocked = !previousCompleted;
                var level = TryExtractLevel(c.Title);

                result.Add(new CourseForMeResponse
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    LanguageCode = c.LanguageCode,
                    Level = level,
                    IsLocked = isLocked,
                    IsCompleted = completion.IsCompleted,
                    CompletionPercent = completion.CompletionPercent
                });

                previousCompleted = completion.IsCompleted;
            }

            return result;
        }

        private static int GetCourseLevelOrder(string title)
        {
            var level = TryExtractLevel(title);

            if (string.IsNullOrWhiteSpace(level))
            {
                return 1000;
            }

            return level switch
            {
                "A1" => 1,
                "A2" => 2,
                "B1" => 3,
                "B2" => 4,
                "C1" => 5,
                "C2" => 6,
                _ => 1000
            };
        }

        private static string? TryExtractLevel(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return null;
            }

            var t = title.ToUpperInvariant();

            // Most common patterns: "English A1", "A1 English", "EN-A1"
            var match = System.Text.RegularExpressions.Regex.Match(t, @"\b([ABC])\s*([12])\b");

            if (!match.Success)
            {
                match = System.Text.RegularExpressions.Regex.Match(t, @"\b([ABC])([12])\b");
            }

            if (!match.Success)
            {
                return null;
            }

            return match.Groups[1].Value + match.Groups[2].Value;
        }

    }
}
