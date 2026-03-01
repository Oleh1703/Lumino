using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Application.Validators;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;

namespace Lumino.Api.Application.Services
{
    public class AdminCourseService : IAdminCourseService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly ICourseStructureValidator _courseStructureValidator;

        public AdminCourseService(LuminoDbContext dbContext, ICourseStructureValidator courseStructureValidator)
        {
            _dbContext = dbContext;
            _courseStructureValidator = courseStructureValidator;
        }

        public List<AdminCourseResponse> GetAll()
        {
            return _dbContext.Courses
                .Select(x => new AdminCourseResponse
                {
                    Id = x.Id,
                    Title = x.Title,
                    Description = x.Description,
                    LanguageCode = x.LanguageCode,
                    IsPublished = x.IsPublished
                })
                .ToList();
        }

        public AdminCourseDetailsResponse GetById(int id)
        {
            var course = _dbContext.Courses.FirstOrDefault(x => x.Id == id);

            if (course == null)
            {
                throw new KeyNotFoundException("Course not found");
            }

            var topics = _dbContext.Topics
                .Where(x => x.CourseId == id)
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.Id)
                .Select(x => new AdminTopicResponse
                {
                    Id = x.Id,
                    CourseId = x.CourseId,
                    Title = x.Title,
                    Order = x.Order
                })
                .ToList();

            var topicIds = topics.Select(x => x.Id).ToList();

            var lessonsCount = _dbContext.Lessons.Count(x => topicIds.Contains(x.TopicId));

            var lessonIds = _dbContext.Lessons
                .Where(x => topicIds.Contains(x.TopicId))
                .Select(x => x.Id)
                .ToList();

            var exercisesCount = _dbContext.Exercises.Count(x => lessonIds.Contains(x.LessonId));

            var scenesQuery = _dbContext.Scenes.Where(x => x.CourseId == id);
            var scenesCount = scenesQuery.Count();

            var scenesPreview = scenesQuery
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.Id)
                .Take(5)
                .Select(x => new AdminSceneResponse
                {
                    Id = x.Id,
                    CourseId = x.CourseId,
                    Title = x.Title,
                    Description = x.Description,
                    SceneType = x.SceneType,
                    BackgroundUrl = x.BackgroundUrl,
                    AudioUrl = x.AudioUrl,
                    Order = x.Order
                })
                .ToList();

            return new AdminCourseDetailsResponse
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                LanguageCode = course.LanguageCode,
                IsPublished = course.IsPublished,
                TopicsCount = topics.Count,
                LessonsCount = lessonsCount,
                ExercisesCount = exercisesCount,
                ScenesCount = scenesCount,
                Topics = topics,
                ScenesPreview = scenesPreview
            };
        }

        public AdminCourseResponse Create(CreateCourseRequest request)
        {
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            var languageCode = string.IsNullOrWhiteSpace(request.LanguageCode)
                ? "en"
                : request.LanguageCode.Trim().ToLowerInvariant();

            if (!SupportedLanguages.IsLearnable(languageCode))
            {
                throw new ArgumentException("LanguageCode is not supported");
            }

            var course = new Course
            {
                Title = request.Title,
                Description = request.Description,
                LanguageCode = languageCode,
                IsPublished = request.IsPublished
            };

            _dbContext.Courses.Add(course);
            _dbContext.SaveChanges();

            return new AdminCourseResponse
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                LanguageCode = course.LanguageCode,
                IsPublished = course.IsPublished
            };
        }

        public void Update(int id, UpdateCourseRequest request)
        {
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            var course = _dbContext.Courses.FirstOrDefault(x => x.Id == id);

            if (course == null)
            {
                throw new KeyNotFoundException("Course not found");
            }

            var languageCode = string.IsNullOrWhiteSpace(request.LanguageCode)
                ? "en"
                : request.LanguageCode.Trim().ToLowerInvariant();

            if (!SupportedLanguages.IsLearnable(languageCode))
            {
                throw new ArgumentException("LanguageCode is not supported");
            }

            course.Title = request.Title;
            course.Description = request.Description;
            course.LanguageCode = languageCode;
            course.IsPublished = request.IsPublished;

            if (request.IsPublished)
            {
                _courseStructureValidator.ValidateOrThrow(course.Id);
            }

            _dbContext.SaveChanges();
        }

        public void Delete(int id)
        {
            var course = _dbContext.Courses.FirstOrDefault(x => x.Id == id);

            if (course == null)
            {
                throw new KeyNotFoundException("Course not found");
            }

            _dbContext.Courses.Remove(course);
            _dbContext.SaveChanges();
        }
    }
}
