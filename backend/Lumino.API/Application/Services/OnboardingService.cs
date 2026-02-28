using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using System;
using System.Linq;

namespace Lumino.Api.Application.Services
{
    public class OnboardingService : IOnboardingService
    {
        private readonly LuminoDbContext _dbContext;

        public OnboardingService(LuminoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<LanguageOptionResponse> GetSupportedLanguages()
        {
            return SupportedLanguages.Learnable
                .Select(x => new LanguageOptionResponse
                {
                    Code = x.Code,
                    Title = x.Title
                })
                .ToList();
        }

        public UserLanguagesResponse GetMyLanguages(int userId)
        {
            var user = _dbContext.Users.FirstOrDefault(x => x.Id == userId);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            var result = new UserLanguagesResponse
            {
                NativeLanguageCode = user.NativeLanguageCode,
                ActiveTargetLanguageCode = user.TargetLanguageCode
            };

            var supported = SupportedLanguages.Learnable
                .ToDictionary(x => SupportedLanguages.Normalize(x.Code), x => x.Title);

            var languageCodes = _dbContext.UserCourses
                .Join(_dbContext.Courses,
                    uc => uc.CourseId,
                    c => c.Id,
                    (uc, c) => new { uc.UserId, uc.IsActive, c.LanguageCode })
                .Where(x => x.UserId == userId)
                .AsEnumerable()
                .Select(x => new { Code = SupportedLanguages.Normalize(x.LanguageCode), x.IsActive })
                .GroupBy(x => x.Code)
                .Select(x => new
                {
                    Code = x.Key,
                    IsActive = string.Equals(x.Key, user.TargetLanguageCode, StringComparison.OrdinalIgnoreCase) || x.Any(z => z.IsActive)
                })
                .ToList();

            if (!string.IsNullOrWhiteSpace(user.TargetLanguageCode))
            {
                var active = SupportedLanguages.Normalize(user.TargetLanguageCode);
                if (languageCodes.Any(x => x.Code == active) == false)
                {
                    languageCodes.Add(new { Code = active, IsActive = true });
                }
            }

            foreach (var lang in languageCodes
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.Code))
            {
                if (supported.TryGetValue(lang.Code, out var title) == false)
                {
                    continue;
                }

                result.LearningLanguages.Add(new UserLearningLanguageResponse
                {
                    Code = lang.Code,
                    Title = title,
                    IsActive = lang.IsActive
                });
            }

            return result;
        }

        public void UpdateMyLanguages(int userId, UpdateUserLanguagesRequest request)
        {
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            SupportedLanguages.ValidateNative(request.NativeLanguageCode, "NativeLanguageCode");
            SupportedLanguages.ValidateLearnable(request.TargetLanguageCode, "TargetLanguageCode");

            var native = SupportedLanguages.Normalize(request.NativeLanguageCode);
            var target = SupportedLanguages.Normalize(request.TargetLanguageCode);

            if (native == target)
            {
                throw new ArgumentException("NativeLanguageCode and TargetLanguageCode must be different");
            }

            var user = _dbContext.Users.FirstOrDefault(x => x.Id == userId);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            user.NativeLanguageCode = native;
            user.TargetLanguageCode = target;

            _dbContext.SaveChanges();

            EnsureActiveCourseForTargetLanguage(userId, target);
}

        public void UpdateMyTargetLanguage(int userId, UpdateTargetLanguageRequest request)
        {
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            SupportedLanguages.ValidateLearnable(request.TargetLanguageCode, "TargetLanguageCode");

            var target = SupportedLanguages.Normalize(request.TargetLanguageCode);

            var user = _dbContext.Users.FirstOrDefault(x => x.Id == userId);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            if (string.IsNullOrWhiteSpace(user.NativeLanguageCode))
            {
                user.NativeLanguageCode = SupportedLanguages.DefaultNativeLanguageCode;
            }

            user.TargetLanguageCode = target;

            _dbContext.SaveChanges();

            EnsureActiveCourseForTargetLanguage(userId, target);
}

        public LanguageAvailabilityResponse GetLanguageAvailability(string languageCode)
        {
            SupportedLanguages.ValidateLearnable(languageCode, "LanguageCode");

            var normalized = SupportedLanguages.Normalize(languageCode);

            var hasCourses = _dbContext.Courses
                .Any(x => x.IsPublished && x.LanguageCode == normalized);

            return new LanguageAvailabilityResponse
            {
                LanguageCode = normalized,
                HasPublishedCourses = hasCourses
            };
        }


        private void EnsureActiveCourseForTargetLanguage(int userId, string targetLanguageCode)
        {
            var course = _dbContext.Courses
                .Where(x => x.IsPublished && x.LanguageCode == targetLanguageCode)
                .OrderByDescending(x => x.Title.Contains("A1"))
                .ThenBy(x => x.Id)
                .FirstOrDefault();

            if (course == null)
            {
                return;
            }

            var myCourses = _dbContext.UserCourses.Where(x => x.UserId == userId).ToList();

            foreach (var c in myCourses)
            {
                c.IsActive = false;
            }

            var existing = myCourses.FirstOrDefault(x => x.CourseId == course.Id);

            if (existing == null)
            {
                _dbContext.UserCourses.Add(new UserCourse
                {
                    UserId = userId,
                    CourseId = course.Id,
                    IsActive = true,
                    IsCompleted = false,
                    StartedAt = DateTime.UtcNow,
                    LastOpenedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.IsActive = true;
                existing.LastOpenedAt = DateTime.UtcNow;
            }

            _dbContext.SaveChanges();
        }
    }
}
