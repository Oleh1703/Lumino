using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Xunit;

namespace Lumino.Tests;

public class OnboardingServiceTests
{
    [Fact]
    public void GetSupportedLanguages_ReturnsList()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new OnboardingService(dbContext);

        var result = service.GetSupportedLanguages();

        Assert.NotEmpty(result);
        Assert.Contains(result, x => x.Code == "en");
        Assert.DoesNotContain(result, x => x.Code == "uk");
    }

    [Fact]
    public void UpdateMyLanguages_SavesToUser()
    {
        var dbContext = TestDbContextFactory.Create();

        var user = new User
        {
            Email = "u@mail.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        dbContext.SaveChanges();

        var service = new OnboardingService(dbContext);

        service.UpdateMyLanguages(user.Id, new UpdateUserLanguagesRequest
        {
            NativeLanguageCode = "pl",
            TargetLanguageCode = "en"
        });

        var fromDb = dbContext.Users.First(x => x.Id == user.Id);
        Assert.Equal("pl", fromDb.NativeLanguageCode);
        Assert.Equal("en", fromDb.TargetLanguageCode);
    }

    [Fact]
    public void UpdateMyLanguages_SameLanguages_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();

        var user = new User
        {
            Email = "u2@mail.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        dbContext.SaveChanges();

        var service = new OnboardingService(dbContext);

        Assert.Throws<ArgumentException>(() =>
        {
            service.UpdateMyLanguages(user.Id, new UpdateUserLanguagesRequest
            {
                NativeLanguageCode = "en",
                TargetLanguageCode = "en"
            });
        });
    }


    [Fact]
    public void GetMyLanguages_ReturnsLearningLanguages_AndActive()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Courses.Add(new Course
        {
            Title = "English A1",
            Description = "desc",
            LanguageCode = "en",
            IsPublished = true
        });

        dbContext.Courses.Add(new Course
        {
            Title = "German A1",
            Description = "desc",
            LanguageCode = "de",
            IsPublished = true
        });

        var user = new User
        {
            Email = "u2@mail.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow,
            NativeLanguageCode = "pl",
            TargetLanguageCode = "en"
        };

        dbContext.Users.Add(user);
        dbContext.SaveChanges();

        dbContext.UserCourses.Add(new UserCourse
        {
            UserId = user.Id,
            CourseId = dbContext.Courses.First(x => x.LanguageCode == "en").Id,
            IsActive = true,
            StartedAt = DateTime.UtcNow,
            LastOpenedAt = DateTime.UtcNow
        });

        dbContext.UserCourses.Add(new UserCourse
        {
            UserId = user.Id,
            CourseId = dbContext.Courses.First(x => x.LanguageCode == "de").Id,
            IsActive = false,
            StartedAt = DateTime.UtcNow,
            LastOpenedAt = DateTime.UtcNow
        });

        dbContext.SaveChanges();

        var service = new OnboardingService(dbContext);

        var result = service.GetMyLanguages(user.Id);

        Assert.Equal("pl", result.NativeLanguageCode);
        Assert.Equal("en", result.ActiveTargetLanguageCode);
        Assert.Contains(result.LearningLanguages, x => x.Code == "en" && x.IsActive);
        Assert.Contains(result.LearningLanguages, x => x.Code == "de" && x.IsActive == false);
    }

}
