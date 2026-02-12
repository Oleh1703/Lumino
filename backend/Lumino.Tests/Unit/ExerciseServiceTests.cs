﻿using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Xunit;

namespace Lumino.Tests;

public class ExerciseServiceTests
{
    [Fact]
    public void GetExercisesByLesson_WhenLessonNotFound_Throws()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new ExerciseService(dbContext);

        Assert.Throws<KeyNotFoundException>(() => service.GetExercisesByLesson(999));
    }

    [Fact]
    public void GetExercisesByLesson_WhenTopicNotFound_Throws()
    {
        var dbContext = TestDbContextFactory.Create();

        // lesson exists, but topic missing
        dbContext.Lessons.Add(new Lesson
        {
            Id = 1,
            TopicId = 100,
            Title = "Lesson",
            Theory = "Theory",
            Order = 1
        });

        dbContext.SaveChanges();

        var service = new ExerciseService(dbContext);

        Assert.Throws<KeyNotFoundException>(() => service.GetExercisesByLesson(1));
    }

    [Fact]
    public void GetExercisesByLesson_WhenCourseNotPublished_Throws()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Courses.Add(new Course
        {
            Id = 1,
            Title = "Course",
            Description = "Desc",
            IsPublished = false
        });

        dbContext.Topics.Add(new Topic
        {
            Id = 1,
            CourseId = 1,
            Title = "Topic",
            Order = 1
        });

        dbContext.Lessons.Add(new Lesson
        {
            Id = 1,
            TopicId = 1,
            Title = "Lesson",
            Theory = "Theory",
            Order = 1
        });

        dbContext.SaveChanges();

        var service = new ExerciseService(dbContext);

        Assert.Throws<KeyNotFoundException>(() => service.GetExercisesByLesson(1));
    }

    [Fact]
    public void GetExercisesByLesson_ReturnsOrderedByOrder_AndTypeAsString()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Courses.Add(new Course
        {
            Id = 1,
            Title = "Course",
            Description = "Desc",
            IsPublished = true
        });

        dbContext.Topics.Add(new Topic
        {
            Id = 1,
            CourseId = 1,
            Title = "Topic",
            Order = 1
        });

        dbContext.Lessons.Add(new Lesson
        {
            Id = 1,
            TopicId = 1,
            Title = "Lesson",
            Theory = "Theory",
            Order = 1
        });

        dbContext.Exercises.AddRange(
            new Exercise
            {
                Id = 1,
                LessonId = 1,
                Type = ExerciseType.Match,
                Question = "Q1",
                Data = "{}",
                CorrectAnswer = "A",
                Order = 2
            },
            new Exercise
            {
                Id = 2,
                LessonId = 1,
                Type = ExerciseType.MultipleChoice,
                Question = "Q2",
                Data = "{}",
                CorrectAnswer = "B",
                Order = 1
            }
        );

        dbContext.SaveChanges();

        var service = new ExerciseService(dbContext);

        var result = service.GetExercisesByLesson(1);

        Assert.Equal(2, result.Count);

        Assert.Equal(2, result[0].Id);
        Assert.Equal("MultipleChoice", result[0].Type);
        Assert.Equal(1, result[0].Order);

        Assert.Equal(1, result[1].Id);
        Assert.Equal("Match", result[1].Type);
        Assert.Equal(2, result[1].Order);
    }
}
