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
                    LanguageCode = x.LanguageCode,
                    Level = x.Level,
                    Order = x.Order,
                    PrerequisiteCourseId = x.PrerequisiteCourseId
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
                .OrderBy(x => GetCourseOrder(x))
                .ThenBy(x => x.Id)
                .ToList();

            var result = new List<CourseForMeResponse>();


            var completionMap = courses
                .ToDictionary(x => x.Id, x => _courseCompletionService.GetMyCourseCompletion(userId, x.Id));


            var inferredPrerequisiteMap = new Dictionary<int, int>();

            foreach (var group in courses.GroupBy(x => x.LanguageCode))
            {
                var ordered = group
                    .OrderBy(x => GetCourseOrder(x))
                    .ThenBy(x => x.Id)
                    .ToList();

                for (var i = 1; i < ordered.Count; i++)
                {
                    var current = ordered[i];

                    if (current.PrerequisiteCourseId == null)
                    {
                        inferredPrerequisiteMap[current.Id] = ordered[i - 1].Id;
                    }
                }
            }

            foreach (var c in courses)
            {
                var completion = completionMap[c.Id];

                var isLocked = IsCourseLockedByPrerequisiteId(GetEffectivePrerequisiteCourseId(c, inferredPrerequisiteMap), completionMap);

                var level = GetCourseLevel(c);

                result.Add(new CourseForMeResponse
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    LanguageCode = c.LanguageCode,
                    Level = level,
                    Order = c.Order,
                    PrerequisiteCourseId = c.PrerequisiteCourseId,
                    IsLocked = isLocked,
                    IsCompleted = completion.IsCompleted,
                    CompletionPercent = completion.CompletionPercent
                });

            }

            return result;
        }


        private static bool IsCourseLocked(Lumino.Api.Domain.Entities.Course course, Dictionary<int, CourseCompletionResponse> completionMap)
        {
            return IsCourseLockedByPrerequisiteId(course.PrerequisiteCourseId, completionMap);
        }


        private static int? GetEffectivePrerequisiteCourseId(Lumino.Api.Domain.Entities.Course course, Dictionary<int, int> inferredPrerequisiteMap)
        {
            if (course.PrerequisiteCourseId != null)
            {
                return course.PrerequisiteCourseId;
            }

            if (inferredPrerequisiteMap.TryGetValue(course.Id, out var inferred))
            {
                return inferred;
            }

            return null;
        }


        private static bool IsCourseLockedByPrerequisiteId(int? prerequisiteCourseId, Dictionary<int, CourseCompletionResponse> completionMap)
        {
            if (prerequisiteCourseId == null)
            {
                return false;
            }

            if (!completionMap.TryGetValue(prerequisiteCourseId.Value, out var prerequisiteCompletion))
            {
                return false;
            }

            return !prerequisiteCompletion.IsCompleted;
        }

        private static string? GetCourseLevel(Lumino.Api.Domain.Entities.Course course)
        {
            if (!string.IsNullOrWhiteSpace(course.Level))
            {
                return course.Level!.Trim().ToUpperInvariant();
            }

            return TryExtractLevel(course.Title);
        }


        private static int GetCourseOrder(Lumino.Api.Domain.Entities.Course course)
        {
            if (course.Order > 0)
            {
                return course.Order;
            }

            var level = GetCourseLevel(course);

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
