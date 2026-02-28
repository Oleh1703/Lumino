using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using System.Text.Json;

namespace Lumino.Api.Application.Services
{
    public class AdminVocabularyService : IAdminVocabularyService
    {
        private readonly LuminoDbContext _dbContext;

        public AdminVocabularyService(LuminoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<AdminVocabularyItemResponse> GetAll()
        {
            var items = _dbContext.VocabularyItems
                .OrderBy(x => x.Id)
                .Select(x => new AdminVocabularyItemResponse
                {
                    Id = x.Id,
                    Word = x.Word,
                    Example = x.Example,
                    PartOfSpeech = x.PartOfSpeech,
                    Definition = x.Definition,
                    Transcription = x.Transcription,
                    Gender = x.Gender
                })
                .ToList();

            if (items.Count == 0)
            {
                return items;
            }

            var ids = items.Select(x => x.Id).ToList();

            var translations = _dbContext.VocabularyItemTranslations
                .Where(x => ids.Contains(x.VocabularyItemId))
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Id)
                .ToList();

            foreach (var item in items)
            {
                item.Translations = translations
                    .Where(x => x.VocabularyItemId == item.Id)
                    .OrderBy(x => x.Order)
                    .Select(x => x.Translation)
                    .ToList();
            }

            return items;
        }

        public AdminVocabularyItemResponse GetById(int id)
        {
            var item = _dbContext.VocabularyItems.FirstOrDefault(x => x.Id == id);

            if (item == null)
            {
                throw new KeyNotFoundException("Vocabulary item not found");
            }

            var translations = _dbContext.VocabularyItemTranslations
                .Where(x => x.VocabularyItemId == id)
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Id)
                .Select(x => x.Translation)
                .ToList();

            return BuildAdminResponse(item);
        }

        public AdminVocabularyItemResponse Create(CreateVocabularyItemRequest request)
        {
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            if (string.IsNullOrWhiteSpace(request.Word))
            {
                throw new ArgumentException("Word is required");
            }

            if (request.Translations == null || request.Translations.Count == 0)
            {
                throw new ArgumentException("Translations are required");
            }

            var cleanTranslations = request.Translations
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (cleanTranslations.Count == 0)
            {
                throw new ArgumentException("Translations are required");
            }

            var item = new VocabularyItem
            {
                Word = request.Word.Trim(),
                Translation = cleanTranslations[0],
                Example = request.Example,
                PartOfSpeech = request.PartOfSpeech,
                Definition = request.Definition,
                Transcription = request.Transcription,
                Gender = request.Gender,
                ExamplesJson = JsonSerializer.Serialize(request.Examples ?? new List<string>()),
                SynonymsJson = JsonSerializer.Serialize(request.Synonyms ?? new List<VocabularyRelationDto>()),
                IdiomsJson = JsonSerializer.Serialize(request.Idioms ?? new List<VocabularyRelationDto>())
            };

            _dbContext.VocabularyItems.Add(item);
            _dbContext.SaveChanges();

            int order = 1;

            foreach (var t in cleanTranslations)
            {
                _dbContext.VocabularyItemTranslations.Add(new VocabularyItemTranslation
                {
                    VocabularyItemId = item.Id,
                    Translation = t,
                    Order = order
                });

                order++;
            }

            _dbContext.SaveChanges();

            return GetById(item.Id);
        }

        public void Update(int id, UpdateVocabularyItemRequest request)
        {
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            var item = _dbContext.VocabularyItems.FirstOrDefault(x => x.Id == id);

            if (item == null)
            {
                throw new KeyNotFoundException("Vocabulary item not found");
            }

            if (string.IsNullOrWhiteSpace(request.Word))
            {
                throw new ArgumentException("Word is required");
            }

            if (request.Translations == null || request.Translations.Count == 0)
            {
                throw new ArgumentException("Translations are required");
            }

            var cleanTranslations = request.Translations
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (cleanTranslations.Count == 0)
            {
                throw new ArgumentException("Translations are required");
            }

            item.Word = request.Word.Trim();
            item.Translation = cleanTranslations[0];
            item.Example = request.Example;

            var existingTranslations = _dbContext.VocabularyItemTranslations
                .Where(x => x.VocabularyItemId == id)
                .ToList();

            if (existingTranslations.Count > 0)
            {
                _dbContext.VocabularyItemTranslations.RemoveRange(existingTranslations);
            }

            _dbContext.SaveChanges();

            int order = 1;

            foreach (var t in cleanTranslations)
            {
                _dbContext.VocabularyItemTranslations.Add(new VocabularyItemTranslation
                {
                    VocabularyItemId = id,
                    Translation = t,
                    Order = order
                });

                order++;
            }

            _dbContext.SaveChanges();
        }

        public void Delete(int id)
        {
            var item = _dbContext.VocabularyItems.FirstOrDefault(x => x.Id == id);

            if (item == null)
            {
                throw new KeyNotFoundException("Vocabulary item not found");
            }

            _dbContext.VocabularyItems.Remove(item);
            _dbContext.SaveChanges();
        }

        public void LinkToLesson(int lessonId, int vocabularyItemId)
        {
            var lessonExists = _dbContext.Lessons.Any(x => x.Id == lessonId);

            if (!lessonExists)
            {
                throw new KeyNotFoundException("Lesson not found");
            }

            var vocabExists = _dbContext.VocabularyItems.Any(x => x.Id == vocabularyItemId);

            if (!vocabExists)
            {
                throw new KeyNotFoundException("Vocabulary item not found");
            }

            bool exists = _dbContext.LessonVocabularies
                .Any(x => x.LessonId == lessonId && x.VocabularyItemId == vocabularyItemId);

            if (exists)
            {
                return;
            }

            _dbContext.LessonVocabularies.Add(new LessonVocabulary
            {
                LessonId = lessonId,
                VocabularyItemId = vocabularyItemId
            });

            _dbContext.SaveChanges();
        }

        public void UnlinkFromLesson(int lessonId, int vocabularyItemId)
        {
            var link = _dbContext.LessonVocabularies
                .FirstOrDefault(x => x.LessonId == lessonId && x.VocabularyItemId == vocabularyItemId);

            if (link == null)
            {
                return;
            }

            _dbContext.LessonVocabularies.Remove(link);
            _dbContext.SaveChanges();
        }

        public List<AdminVocabularyItemResponse> GetByLesson(int lessonId)
        {
            var lessonExists = _dbContext.Lessons.Any(x => x.Id == lessonId);

            if (!lessonExists)
            {
                throw new KeyNotFoundException("Lesson not found");
            }

            var ids = _dbContext.LessonVocabularies
                .Where(x => x.LessonId == lessonId)
                .Select(x => x.VocabularyItemId)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
            {
                return new List<AdminVocabularyItemResponse>();
            }

            var items = _dbContext.VocabularyItems
                .Where(x => ids.Contains(x.Id))
                .Select(x => new AdminVocabularyItemResponse
                {
                    Id = x.Id,
                    Word = x.Word,
                    Example = x.Example,
                    PartOfSpeech = x.PartOfSpeech,
                    Definition = x.Definition,
                    Transcription = x.Transcription,
                    Gender = x.Gender
                })
                .ToList();

            var translations = _dbContext.VocabularyItemTranslations
                .Where(x => ids.Contains(x.VocabularyItemId))
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Id)
                .ToList();

            foreach (var item in items)
            {
                item.Translations = translations
                    .Where(x => x.VocabularyItemId == item.Id)
                    .OrderBy(x => x.Order)
                    .Select(x => x.Translation)
                    .ToList();
            }

            return items;
        }


        private AdminVocabularyItemResponse BuildAdminResponse(VocabularyItem item)
        {
            var translations = _dbContext.VocabularyItemTranslations
                .Where(x => x.VocabularyItemId == item.Id)
                .OrderBy(x => x.Order)
                .Select(x => x.Translation)
                .ToList();

            if (translations.Count == 0 && !string.IsNullOrWhiteSpace(item.Translation))
            {
                translations.Add(item.Translation);
            }

            var examples = DeserializeOrEmpty<List<string>>(item.ExamplesJson);

            if (examples.Count == 0 && !string.IsNullOrWhiteSpace(item.Example))
            {
                examples.Add(item.Example);
            }

            return new AdminVocabularyItemResponse
            {
                Id = item.Id,
                Word = item.Word,
                Example = item.Example,
                Translations = translations,
                PartOfSpeech = item.PartOfSpeech,
                Definition = item.Definition,
                Transcription = item.Transcription,
                Gender = item.Gender,
                Examples = examples,
                Synonyms = DeserializeOrEmpty<List<VocabularyRelationDto>>(item.SynonymsJson),
                Idioms = DeserializeOrEmpty<List<VocabularyRelationDto>>(item.IdiomsJson)
            };
        }

        private static T DeserializeOrEmpty<T>(string? json) where T : new()
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new T();
            }

            try
            {
                var value = JsonSerializer.Deserialize<T>(json);
                return value ?? new T();
            }
            catch
            {
                return new T();
            }
        }
    }
}
