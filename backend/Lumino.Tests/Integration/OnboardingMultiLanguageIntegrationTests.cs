using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Xunit;

namespace Lumino.Tests.Integration;

public class OnboardingMultiLanguageIntegrationTests
{
    [Fact]
    public void UpdateTargetLanguage_ShouldKeepPreviousLanguages_AndSwitchActive()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.Add(new User
        {
            Id = 1,
            Email = "user@test.com",
            PasswordHash = "hash",
            IsEmailVerified = true,
            Role = Lumino.Api.Domain.Enums.Role.User,
            CreatedAt = DateTime.UtcNow,
            NativeLanguageCode = "uk",
            TargetLanguageCode = "en"
        });

        dbContext.Courses.Add(new Course
        {
            Id = 1,
            Title = "English A1",
            LanguageCode = "en",
            IsPublished = true
        });

        dbContext.Courses.Add(new Course
        {
            Id = 2,
            Title = "German A1",
            LanguageCode = "de",
            IsPublished = true
        });

        dbContext.SaveChanges();

        var onboarding = new OnboardingService(dbContext);

        onboarding.UpdateMyTargetLanguage(1, new UpdateTargetLanguageRequest
        {
            TargetLanguageCode = "en"
        });

        onboarding.UpdateMyTargetLanguage(1, new UpdateTargetLanguageRequest
        {
            TargetLanguageCode = "de"
        });

        var me = onboarding.GetMyLanguages(1);

        Assert.Equal("uk", me.NativeLanguageCode);
        Assert.Equal("de", me.ActiveTargetLanguageCode);

        Assert.Contains(me.LearningLanguages, x => x.Code == "en");
        Assert.Contains(me.LearningLanguages, x => x.Code == "de");

        var active = me.LearningLanguages.FirstOrDefault(x => x.IsActive);

        Assert.NotNull(active);
        Assert.Equal("de", active!.Code);
    }
}
