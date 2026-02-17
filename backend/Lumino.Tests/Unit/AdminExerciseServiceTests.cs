﻿using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Xunit;

namespace Lumino.Tests;

public class AdminExerciseServiceTests
{
    [Fact]
    public void Create_MultipleChoice_InvalidJson_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();
        SeedLesson(dbContext);

        var service = new AdminExerciseService(dbContext);

        Assert.Throws<ArgumentException>(() =>
        {
            service.Create(new CreateExerciseRequest
            {
                LessonId = 1,
                Type = "MultipleChoice",
                Question = "Q",
                Data = "NOT_JSON",
                CorrectAnswer = "A",
                Order = 1
            });
        });
    }

    [Fact]
    public void Create_MultipleChoice_CorrectAnswerNotInOptions_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();
        SeedLesson(dbContext);

        var service = new AdminExerciseService(dbContext);

        Assert.Throws<ArgumentException>(() =>
        {
            service.Create(new CreateExerciseRequest
            {
                LessonId = 1,
                Type = "MultipleChoice",
                Question = "Q",
                Data = "[\"A\",\"B\"]",
                CorrectAnswer = "C",
                Order = 1
            });
        });
    }

    [Fact]
    public void Create_Match_InvalidJson_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();
        SeedLesson(dbContext);

        var service = new AdminExerciseService(dbContext);

        Assert.Throws<ArgumentException>(() =>
        {
            service.Create(new CreateExerciseRequest
            {
                LessonId = 1,
                Type = "Match",
                Question = "Q",
                Data = "NOT_JSON",
                CorrectAnswer = "{}",
                Order = 1
            });
        });
    }

    [Fact]
    public void Create_Match_DuplicateLeft_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();
        SeedLesson(dbContext);

        var service = new AdminExerciseService(dbContext);

        Assert.Throws<ArgumentException>(() =>
        {
            service.Create(new CreateExerciseRequest
            {
                LessonId = 1,
                Type = "Match",
                Question = "Q",
                Data = "[{ \"left\": \"cat\", \"right\": \"кіт\" },{ \"left\": \"cat\", \"right\": \"пес\" }]",
                CorrectAnswer = "{}",
                Order = 1
            });
        });
    }

    [Fact]
    public void Create_Match_ValidData_ShouldCreate()
    {
        var dbContext = TestDbContextFactory.Create();
        SeedLesson(dbContext);

        var service = new AdminExerciseService(dbContext);

        var result = service.Create(new CreateExerciseRequest
        {
            LessonId = 1,
            Type = "Match",
            Question = "Match words",
            Data = "[{ \"left\": \"cat\", \"right\": \"кіт\" },{ \"left\": \"dog\", \"right\": \"пес\" }]",
            CorrectAnswer = "{}",
            Order = 1
        });

        Assert.True(result.Id > 0);
        Assert.Equal(1, result.LessonId);
        Assert.Equal("Match", result.Type);
    }

    
    [Fact]
    public void Create_OrderZero_ShouldCreate_AndBeLastInGetByLesson()
    {
        var dbContext = TestDbContextFactory.Create();
        SeedLesson(dbContext);

        var service = new AdminExerciseService(dbContext);

        service.Create(new CreateExerciseRequest
        {
            LessonId = 1,
            Type = "Input",
            Question = "Q1",
            Data = "{}",
            CorrectAnswer = "A",
            Order = 1
        });

        var result = service.Create(new CreateExerciseRequest
        {
            LessonId = 1,
            Type = "Input",
            Question = "Q2",
            Data = "{}",
            CorrectAnswer = "B",
            Order = 0
        });

        var list = service.GetByLesson(1);

        Assert.Equal(2, list.Count);
        Assert.Equal(1, list[0].Order);
        Assert.Equal(result.Id, list[1].Id);
        Assert.Equal(0, list[1].Order);
    }

    [Fact]
    public void Create_DuplicatePositiveOrderInLesson_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();
        SeedLesson(dbContext);

        var service = new AdminExerciseService(dbContext);

        service.Create(new CreateExerciseRequest
        {
            LessonId = 1,
            Type = "Input",
            Question = "Q1",
            Data = "{}",
            CorrectAnswer = "A",
            Order = 1
        });

        Assert.Throws<ArgumentException>(() =>
        {
            service.Create(new CreateExerciseRequest
            {
                LessonId = 1,
                Type = "Input",
                Question = "Q2",
                Data = "{}",
                CorrectAnswer = "B",
                Order = 1
            });
        });
    }

private static void SeedLesson(Lumino.Api.Data.LuminoDbContext dbContext)
    {
        dbContext.Courses.Add(new Course
        {
            Id = 1,
            Title = "English A1",
            Description = "Desc",
            IsPublished = true
        });

        dbContext.Topics.Add(new Topic
        {
            Id = 1,
            CourseId = 1,
            Title = "Basics",
            Order = 1
        });

        dbContext.Lessons.Add(new Lesson
        {
            Id = 1,
            TopicId = 1,
            Title = "Lesson 1",
            Theory = "T",
            Order = 1
        });

        dbContext.SaveChanges();
    }
}
