using Lumino.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Lumino.Api.Application.Validators
{
    public class CourseStructureValidator : ICourseStructureValidator
    {
        private const int TopicsPerCourse = 10;
        private const int LessonsPerTopic = 8;
        private const int ExercisesPerLesson = 9;

        private const string FinalSceneType = "Sun";

        private readonly LuminoDbContext _dbContext;

        public CourseStructureValidator(LuminoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void ValidateOrThrow(int courseId)
        {
            var course = _dbContext.Courses
                .AsNoTracking()
                .FirstOrDefault(x => x.Id == courseId);

            if (course == null)
            {
                throw new KeyNotFoundException("Course not found");
            }

            var topics = _dbContext.Topics
                .AsNoTracking()
                .Where(x => x.CourseId == courseId)
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Id)
                .ToList();

            if (topics.Count != TopicsPerCourse)
            {
                throw new ArgumentException($"Course structure error: course must have exactly {TopicsPerCourse} topics, but found {topics.Count}");
            }

            var topicOrders = topics
                .Select(x => x.Order)
                .ToList();

            if (topicOrders.Distinct().Count() != TopicsPerCourse || topicOrders.Min() != 1 || topicOrders.Max() != TopicsPerCourse)
            {
                throw new ArgumentException($"Course structure error: topics order must be unique and exactly 1..{TopicsPerCourse}");
            }

            foreach (var topic in topics)
            {
                var lessons = _dbContext.Lessons
                    .AsNoTracking()
                    .Where(x => x.TopicId == topic.Id)
                    .OrderBy(x => x.Order)
                    .ThenBy(x => x.Id)
                    .ToList();

                if (lessons.Count != LessonsPerTopic)
                {
                    throw new ArgumentException($"Course structure error: topic #{topic.Order} must have exactly {LessonsPerTopic} lessons, but found {lessons.Count}");
                }

                var lessonOrders = lessons
                    .Select(x => x.Order)
                    .ToList();

                if (lessonOrders.Distinct().Count() != LessonsPerTopic || lessonOrders.Min() != 1 || lessonOrders.Max() != LessonsPerTopic)
                {
                    throw new ArgumentException($"Course structure error: lessons order in topic #{topic.Order} must be unique and exactly 1..{LessonsPerTopic}");
                }

                var finalScenesCount = _dbContext.Scenes
                    .AsNoTracking()
                    .Count(x => x.TopicId == topic.Id && x.SceneType == FinalSceneType);

                if (finalScenesCount != 1)
                {
                    throw new ArgumentException($"Course structure error: topic #{topic.Order} must have exactly 1 final scene with SceneType='{FinalSceneType}', but found {finalScenesCount}");
                }

                foreach (var lesson in lessons)
                {
                    var exercises = _dbContext.Exercises
                        .AsNoTracking()
                        .Where(x => x.LessonId == lesson.Id)
                        .OrderBy(x => x.Order)
                        .ThenBy(x => x.Id)
                        .ToList();

                    if (exercises.Count != ExercisesPerLesson)
                    {
                        throw new ArgumentException($"Course structure error: lesson #{lesson.Order} in topic #{topic.Order} must have exactly {ExercisesPerLesson} exercises, but found {exercises.Count}");
                    }

                    var exerciseOrders = exercises
                        .Select(x => x.Order)
                        .ToList();

                    if (exerciseOrders.Distinct().Count() != ExercisesPerLesson || exerciseOrders.Min() != 1 || exerciseOrders.Max() != ExercisesPerLesson)
                    {
                        throw new ArgumentException($"Course structure error: exercises order in lesson #{lesson.Order} (topic #{topic.Order}) must be unique and exactly 1..{ExercisesPerLesson}");
                    }
                }
            }
        }
    }
}
