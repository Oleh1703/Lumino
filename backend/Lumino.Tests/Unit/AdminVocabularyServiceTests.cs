using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Xunit;

namespace Lumino.Tests;

public class AdminVocabularyServiceTests
{
    [Fact]
    public void Create_AddsItemWithTranslations()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new AdminVocabularyService(dbContext);

        var created = service.Create(new CreateVocabularyItemRequest
        {
            Word = "cat",
            Example = "A cat",
            Translations = new() { "кіт", "котик", "кіт" }
        });

        Assert.True(created.Id > 0);
        Assert.Equal("cat", created.Word);
        Assert.Equal(2, created.Translations.Count);

        var savedItem = dbContext.VocabularyItems.FirstOrDefault(x => x.Id == created.Id);
        Assert.NotNull(savedItem);

        var savedTranslations = dbContext.VocabularyItemTranslations
            .Where(x => x.VocabularyItemId == created.Id)
            .OrderBy(x => x.Order)
            .ToList();

        Assert.Equal(2, savedTranslations.Count);
        Assert.Equal("кіт", savedTranslations[0].Translation);
        Assert.Equal("котик", savedTranslations[1].Translation);
    }

    [Fact]
    public void Update_ReplacesTranslations()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new AdminVocabularyService(dbContext);

        var created = service.Create(new CreateVocabularyItemRequest
        {
            Word = "dog",
            Example = "A dog",
            Translations = new() { "пес", "собака" }
        });

        service.Update(created.Id, new UpdateVocabularyItemRequest
        {
            Word = "dog",
            Example = "A dog!",
            Translations = new() { "песик" }
        });

        var updated = service.GetById(created.Id);

        Assert.Equal("dog", updated.Word);
        Assert.Equal("A dog!", updated.Example);
        Assert.Single(updated.Translations);
        Assert.Equal("песик", updated.Translations[0]);
    }

    [Fact]
    public void Update_UpdatesDictionaryFields()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new AdminVocabularyService(dbContext);

        var created = service.Create(new CreateVocabularyItemRequest
        {
            Word = "house",
            Example = "A house",
            Translations = new() { "будинок" },
            PartOfSpeech = "noun",
            Definition = "A building where people live",
            Transcription = "/haʊs/",
            Gender = "none",
            Examples = new() { "This house is big." },
            Synonyms = new()
            {
                new VocabularyRelationDto { Word = "home", Translation = "дім" }
            },
            Idioms = new()
            {
                new VocabularyRelationDto { Word = "bring the house down", Translation = "зірвати оплески" }
            }
        });

        service.Update(created.Id, new UpdateVocabularyItemRequest
        {
            Word = "house",
            Example = "A house!",
            Translations = new() { "будинок", "хата" },
            PartOfSpeech = "noun",
            Definition = "A place where people live",
            Transcription = "/haʊs/",
            Gender = "none",
            Examples = new() { "This house is small.", "This is my house." },
            Synonyms = new()
            {
                new VocabularyRelationDto { Word = "home", Translation = "дім" },
                new VocabularyRelationDto { Word = "dwelling", Translation = "житло" }
            },
            Idioms = new()
            {
                new VocabularyRelationDto { Word = "on the house", Translation = "за рахунок закладу" }
            }
        });

        var updated = service.GetById(created.Id);

        Assert.Equal("house", updated.Word);
        Assert.Equal("A house!", updated.Example);
        Assert.Equal(2, updated.Translations.Count);
        Assert.Equal("будинок", updated.Translations[0]);
        Assert.Equal("хата", updated.Translations[1]);

        Assert.Equal("noun", updated.PartOfSpeech);
        Assert.Equal("A place where people live", updated.Definition);
        Assert.Equal("/haʊs/", updated.Transcription);
        Assert.Equal("none", updated.Gender);

        Assert.Equal(2, updated.Examples.Count);
        Assert.Equal("This house is small.", updated.Examples[0]);
        Assert.Equal("This is my house.", updated.Examples[1]);

        Assert.Equal(2, updated.Synonyms.Count);
        Assert.Equal("home", updated.Synonyms[0].Word);
        Assert.Equal("dwelling", updated.Synonyms[1].Word);

        Assert.Single(updated.Idioms);
        Assert.Equal("on the house", updated.Idioms[0].Word);

        var savedItem = dbContext.VocabularyItems.FirstOrDefault(x => x.Id == created.Id);
        Assert.NotNull(savedItem);
        Assert.False(string.IsNullOrWhiteSpace(savedItem!.ExamplesJson));
        Assert.False(string.IsNullOrWhiteSpace(savedItem.SynonymsJson));
        Assert.False(string.IsNullOrWhiteSpace(savedItem.IdiomsJson));
    }

    [Fact]
    public void LinkAndUnlink_LessonVocabulary_Works()
    {
        var dbContext = TestDbContextFactory.Create();

        var topic = new Topic { CourseId = 1, Title = "Topic", Order = 1 };
        dbContext.Topics.Add(topic);
        dbContext.SaveChanges();

        var lesson = new Lesson { TopicId = topic.Id, Title = "L1", Theory = "T1", Order = 1 };
        dbContext.Lessons.Add(lesson);
        dbContext.SaveChanges();

        var vocab = new VocabularyItem { Word = "tree", Translation = "дерево", Example = "tree" };
        dbContext.VocabularyItems.Add(vocab);
        dbContext.SaveChanges();

        var service = new AdminVocabularyService(dbContext);

        service.LinkToLesson(lesson.Id, vocab.Id);

        var list = service.GetByLesson(lesson.Id);

        Assert.Single(list);
        Assert.Equal(vocab.Id, list[0].Id);

        service.UnlinkFromLesson(lesson.Id, vocab.Id);

        var list2 = service.GetByLesson(lesson.Id);

        Assert.Empty(list2);
    }

    [Fact]
    public void Delete_RemovesItemAndTranslations()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new AdminVocabularyService(dbContext);

        var created = service.Create(new CreateVocabularyItemRequest
        {
            Word = "bird",
            Example = "A bird",
            Translations = new() { "птах" }
        });

        service.Delete(created.Id);

        Assert.False(dbContext.VocabularyItems.Any(x => x.Id == created.Id));
        Assert.False(dbContext.VocabularyItemTranslations.Any(x => x.VocabularyItemId == created.Id));
    }
}
