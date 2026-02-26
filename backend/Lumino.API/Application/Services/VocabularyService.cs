using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumino.Api.Application.Services
{
    public class VocabularyService : IVocabularyService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly LearningSettings _learningSettings;

        public VocabularyService(LuminoDbContext dbContext, IDateTimeProvider dateTimeProvider, IOptions<LearningSettings> learningSettings)
        {
            _dbContext = dbContext;
            _dateTimeProvider = dateTimeProvider;
            _learningSettings = learningSettings?.Value ?? new LearningSettings();
        }

        public List<VocabularyResponse> GetMyVocabulary(int userId)
        {
            var query =
                from uv in _dbContext.UserVocabularies
                join vi in _dbContext.VocabularyItems on uv.VocabularyItemId equals vi.Id
                where uv.UserId == userId
                orderby uv.AddedAt descending
                select new VocabularyResponse
                {
                    Id = uv.Id,
                    VocabularyItemId = vi.Id,
                    Word = vi.Word,
                    Translation = vi.Translation,
                    Example = vi.Example,
                    AddedAt = uv.AddedAt,
                    LastReviewedAt = uv.LastReviewedAt,
                    NextReviewAt = uv.NextReviewAt,
                    ReviewCount = uv.ReviewCount
                };

            var list = query.ToList();

            ApplyTranslations(list);

            return list;
        }

        public List<VocabularyResponse> GetDueVocabulary(int userId)
        {
            var now = _dateTimeProvider.UtcNow;

            var query =
                from uv in _dbContext.UserVocabularies
                join vi in _dbContext.VocabularyItems on uv.VocabularyItemId equals vi.Id
                where uv.UserId == userId && uv.NextReviewAt <= now
                orderby uv.NextReviewAt, uv.AddedAt
                select new VocabularyResponse
                {
                    Id = uv.Id,
                    VocabularyItemId = vi.Id,
                    Word = vi.Word,
                    Translation = vi.Translation,
                    Example = vi.Example,
                    AddedAt = uv.AddedAt,
                    LastReviewedAt = uv.LastReviewedAt,
                    NextReviewAt = uv.NextReviewAt,
                    ReviewCount = uv.ReviewCount
                };

            var list = query.ToList();

            ApplyTranslations(list);

            return list;
        }

        public VocabularyResponse? GetNextReview(int userId)
        {
            var now = _dateTimeProvider.UtcNow;

            var entity =
                _dbContext.UserVocabularies
                    .Where(x => x.UserId == userId && x.NextReviewAt <= now)
                    .OrderBy(x => x.NextReviewAt)
                    .ThenBy(x => x.AddedAt)
                    .FirstOrDefault();

            if (entity == null)
            {
                return null;
            }

            var item = _dbContext.VocabularyItems.First(x => x.Id == entity.VocabularyItemId);

            var response = new VocabularyResponse
            {
                Id = entity.Id,
                VocabularyItemId = item.Id,
                Word = item.Word,
                Translation = item.Translation,
                Example = item.Example,
                AddedAt = entity.AddedAt,
                LastReviewedAt = entity.LastReviewedAt,
                NextReviewAt = entity.NextReviewAt,
                ReviewCount = entity.ReviewCount
            };

            ApplyTranslations(response);

            return response;
        }

        public void AddWord(int userId, AddVocabularyRequest request)
        {
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            if (string.IsNullOrWhiteSpace(request.Word))
            {
                throw new ArgumentException("Word is required");
            }

            if (string.IsNullOrWhiteSpace(request.Translation))
            {
                throw new ArgumentException("Translation is required");
            }

            var word = request.Word.Trim();

            var translations = NormalizeTranslations(request.Translation, request.Translations);
            var primaryTranslation = translations[0];

            var item = FindOrCreateVocabularyItem(word, primaryTranslation, request.Example);

            EnsureTranslations(item.Id, primaryTranslation, translations);

            var alreadyAdded = _dbContext.UserVocabularies
                .Any(x => x.UserId == userId && x.VocabularyItemId == item.Id);

            if (alreadyAdded)
            {
                return;
            }

            var now = _dateTimeProvider.UtcNow;

            var userWord = new UserVocabulary
            {
                UserId = userId,
                VocabularyItemId = item.Id,
                AddedAt = now,
                LastReviewedAt = null,
                NextReviewAt = now,
                ReviewCount = 0
            };

            _dbContext.UserVocabularies.Add(userWord);
            _dbContext.SaveChanges();
        }

        public VocabularyResponse ReviewWord(int userId, int userVocabularyId, ReviewVocabularyRequest request)
        {
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            var entity = _dbContext.UserVocabularies
                .FirstOrDefault(x => x.Id == userVocabularyId && x.UserId == userId);

            if (entity == null)
            {
                throw new KeyNotFoundException("Vocabulary word not found");
            }


            var idempotencyKey = request.IdempotencyKey;

            if (string.IsNullOrWhiteSpace(idempotencyKey) == false)
            {
                if (entity.ReviewIdempotencyKey == idempotencyKey)
                {
                    var existingItem = _dbContext.VocabularyItems.First(x => x.Id == entity.VocabularyItemId);

                    var existingResponse = new VocabularyResponse
                    {
                        Id = entity.Id,
                        VocabularyItemId = existingItem.Id,
                        Word = existingItem.Word,
                        Translation = existingItem.Translation,
                        Example = existingItem.Example,
                        AddedAt = entity.AddedAt,
                        LastReviewedAt = entity.LastReviewedAt,
                        NextReviewAt = entity.NextReviewAt,
                        ReviewCount = entity.ReviewCount
                    };

                    ApplyTranslations(existingResponse);

                    return existingResponse;
                }
            }
            var now = _dateTimeProvider.UtcNow;

            entity.LastReviewedAt = now;

            if (request.IsCorrect)
            {
                entity.ReviewCount = entity.ReviewCount + 1;
                entity.NextReviewAt = CalculateNextReviewAt(now, entity.ReviewCount);
            }
            else
            {
                entity.ReviewCount = 0;
                entity.NextReviewAt = now.AddHours(_learningSettings.VocabularyWrongDelayHours);
            }


            if (string.IsNullOrWhiteSpace(idempotencyKey) == false)
            {
                entity.ReviewIdempotencyKey = idempotencyKey;
            }

            _dbContext.SaveChanges();

            var item = _dbContext.VocabularyItems.First(x => x.Id == entity.VocabularyItemId);

            var response = new VocabularyResponse
            {
                Id = entity.Id,
                VocabularyItemId = item.Id,
                Word = item.Word,
                Translation = item.Translation,
                Example = item.Example,
                AddedAt = entity.AddedAt,
                LastReviewedAt = entity.LastReviewedAt,
                NextReviewAt = entity.NextReviewAt,
                ReviewCount = entity.ReviewCount
            };

            ApplyTranslations(response);

            return response;
        }

        public void DeleteWord(int userId, int userVocabularyId)
        {
            var entity = _dbContext.UserVocabularies
                .FirstOrDefault(x => x.Id == userVocabularyId && x.UserId == userId);

            if (entity == null)
            {
                throw new KeyNotFoundException("Vocabulary word not found");
            }

            _dbContext.UserVocabularies.Remove(entity);
            _dbContext.SaveChanges();
        }


        private void ApplyTranslations(List<VocabularyResponse> list)
        {
            if (list == null || list.Count == 0)
            {
                return;
            }

            var ids = list.Select(x => x.VocabularyItemId).Distinct().ToList();

            var translations = _dbContext.VocabularyItemTranslations
                .Where(x => ids.Contains(x.VocabularyItemId))
                .OrderBy(x => x.VocabularyItemId)
                .ThenBy(x => x.Order)
                .ToList();

            var map = translations
                .GroupBy(x => x.VocabularyItemId)
                .ToDictionary(x => x.Key, x => x.Select(t => t.Translation).ToList());

            foreach (var item in list)
            {
                if (map.TryGetValue(item.VocabularyItemId, out var listTranslations) && listTranslations.Count > 0)
                {
                    item.Translations = listTranslations;
                    item.Translation = listTranslations[0];
                }
                else
                {
                    item.Translations = new List<string> { item.Translation };
                }
            }
        }

        private void ApplyTranslations(VocabularyResponse item)
        {
            if (item == null)
            {
                return;
            }

            var list = _dbContext.VocabularyItemTranslations
                .Where(x => x.VocabularyItemId == item.VocabularyItemId)
                .OrderBy(x => x.Order)
                .Select(x => x.Translation)
                .ToList();

            if (list.Count > 0)
            {
                item.Translations = list;
                item.Translation = list[0];
            }
            else
            {
                item.Translations = new List<string> { item.Translation };
            }
        }

        private List<string> NormalizeTranslations(string translation, List<string>? translations)
        {
            var list = new List<string>();

            if (!string.IsNullOrWhiteSpace(translation))
            {
                list.Add(translation.Trim());
            }

            if (translations != null && translations.Count > 0)
            {
                foreach (var t in translations)
                {
                    if (string.IsNullOrWhiteSpace(t))
                    {
                        continue;
                    }

                    list.Add(t.Trim());
                }
            }

            list = list
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (list.Count == 0)
            {
                throw new ArgumentException("Translation is required");
            }

            return list;
        }

        private void EnsureTranslations(int vocabularyItemId, string primaryTranslation, List<string> translations)
        {
            if (translations == null || translations.Count == 0)
            {
                return;
            }

            var existing = _dbContext.VocabularyItemTranslations
                .Where(x => x.VocabularyItemId == vocabularyItemId)
                .OrderBy(x => x.Order)
                .ToList();

            if (existing.Count == 0)
            {
                for (var i = 0; i < translations.Count; i++)
                {
                    _dbContext.VocabularyItemTranslations.Add(new VocabularyItemTranslation
                    {
                        VocabularyItemId = vocabularyItemId,
                        Translation = translations[i],
                        Order = i
                    });
                }

                _dbContext.SaveChanges();
                EnsurePrimaryTranslation(vocabularyItemId, primaryTranslation);
                return;
            }

            var existingValues = existing
                .Select(x => x.Translation)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var nextOrder = existing.Max(x => x.Order) + 1;

            foreach (var t in translations)
            {
                if (existingValues.Contains(t))
                {
                    continue;
                }

                _dbContext.VocabularyItemTranslations.Add(new VocabularyItemTranslation
                {
                    VocabularyItemId = vocabularyItemId,
                    Translation = t,
                    Order = nextOrder
                });

                nextOrder++;
            }

            _dbContext.SaveChanges();
            EnsurePrimaryTranslation(vocabularyItemId, primaryTranslation);
        }

        private VocabularyItem FindOrCreateVocabularyItem(string word, string primaryTranslation, string? example)
        {
            // We identify a vocabulary item by Word.
            // If the same word is added later with another primary translation,
            // it must extend the existing translations list.
            var normalizedWord = word.ToLower();

            var candidates = _dbContext.VocabularyItems
                .Where(x => x.Word.ToLower() == normalizedWord)
                .OrderBy(x => x.Id)
                .ToList();

            if (candidates.Count == 0)
            {
                var created = new VocabularyItem
                {
                    Word = word,
                    Translation = primaryTranslation,
                    Example = example
                };

                _dbContext.VocabularyItems.Add(created);
                _dbContext.SaveChanges();

                return created;
            }

            if (candidates.Count == 1)
            {
                return candidates[0];
            }

            // If duplicates exist in DB, prefer an item whose Translation matches the primary.
            var normalizedPrimary = primaryTranslation.ToLower();
            var exact = candidates.FirstOrDefault(x => x.Translation.ToLower() == normalizedPrimary);
            return exact ?? candidates[0];
        }

        private void EnsurePrimaryTranslation(int vocabularyItemId, string primaryTranslation)
        {
            if (string.IsNullOrWhiteSpace(primaryTranslation))
            {
                return;
            }

            var normalizedPrimary = primaryTranslation.Trim();

            var translations = _dbContext.VocabularyItemTranslations
                .Where(x => x.VocabularyItemId == vocabularyItemId)
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Id)
                .ToList();

            if (translations.Count == 0)
            {
                return;
            }

            var hasChanges = false;

            // Remove duplicated translation rows (same text ignoring case).
            var duplicates = translations
                .GroupBy(x => x.Translation, StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .ToList();

            if (duplicates.Count > 0)
            {
                foreach (var g in duplicates)
                {
                    var keep = g
                        .OrderBy(x => x.Order)
                        .ThenBy(x => x.Id)
                        .First();

                    var remove = g
                        .Where(x => x.Id != keep.Id)
                        .ToList();

                    if (remove.Count > 0)
                    {
                        _dbContext.VocabularyItemTranslations.RemoveRange(remove);
                        hasChanges = true;
                    }
                }

                if (hasChanges)
                {
                    _dbContext.SaveChanges();

                    translations = _dbContext.VocabularyItemTranslations
                        .Where(x => x.VocabularyItemId == vocabularyItemId)
                        .OrderBy(x => x.Order)
                        .ThenBy(x => x.Id)
                        .ToList();
                }
            }

            // Ensure primary exists.
            var primaryEntity = translations
                .FirstOrDefault(x => string.Equals(x.Translation, normalizedPrimary, StringComparison.OrdinalIgnoreCase));

            if (primaryEntity == null)
            {
                // Shift all orders by +1 to keep unique index safe (Order must be unique per item).
                foreach (var t in translations)
                {
                    t.Order = t.Order + 1;
                }

                primaryEntity = new VocabularyItemTranslation
                {
                    VocabularyItemId = vocabularyItemId,
                    Translation = normalizedPrimary,
                    Order = 0
                };

                _dbContext.VocabularyItemTranslations.Add(primaryEntity);

                hasChanges = true;

                _dbContext.SaveChanges();

                translations = _dbContext.VocabularyItemTranslations
                    .Where(x => x.VocabularyItemId == vocabularyItemId)
                    .OrderBy(x => x.Order)
                    .ThenBy(x => x.Id)
                    .ToList();

                primaryEntity = translations
                    .FirstOrDefault(x => string.Equals(x.Translation, normalizedPrimary, StringComparison.OrdinalIgnoreCase));
            }

            if (primaryEntity == null)
            {
                return;
            }

            // Build a deterministic order: primary first, then others by existing Order.
            var ordered = translations
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Id)
                .ToList();

            var newOrder = new List<VocabularyItemTranslation> { primaryEntity };

            foreach (var t in ordered)
            {
                if (t.Id == primaryEntity.Id)
                {
                    continue;
                }

                newOrder.Add(t);
            }

            // Apply sequential order values (0..n-1) to guarantee unique (VocabularyItemId, Order).
            for (var i = 0; i < newOrder.Count; i++)
            {
                if (newOrder[i].Order != i)
                {
                    newOrder[i].Order = i;
                    hasChanges = true;
                }
            }

            var item = _dbContext.VocabularyItems.First(x => x.Id == vocabularyItemId);

            if (string.Equals(item.Translation, primaryEntity.Translation, StringComparison.OrdinalIgnoreCase) == false)
            {
                item.Translation = primaryEntity.Translation;
                hasChanges = true;
            }

            if (hasChanges)
            {
                _dbContext.SaveChanges();
            }
        }


        private DateTime CalculateNextReviewAt(DateTime now, int reviewCount)
        {
            var intervals = _learningSettings.VocabularyReviewIntervalsDays;

            if (intervals == null || intervals.Count == 0)
            {
                intervals = new List<int> { 1, 2, 4, 7, 14, 30, 60 };
            }

            if (reviewCount <= 0)
            {
                return now;
            }

            var index = reviewCount - 1;

            var days = index < intervals.Count
                ? intervals[index]
                : intervals[intervals.Count - 1];

            return now.AddDays(days);
        }
    

        public VocabularyItemDetailsResponse GetItemDetails(int userId, int vocabularyItemId)
        {
            var item = _dbContext.VocabularyItems.FirstOrDefault(x => x.Id == vocabularyItemId);

            if (item == null)
            {
                throw new KeyNotFoundException("Vocabulary item not found");
            }

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

            return new VocabularyItemDetailsResponse
            {
                Id = item.Id,
                Word = item.Word,
                Translations = translations,
                PartOfSpeech = item.PartOfSpeech,
                Definition = item.Definition,
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
