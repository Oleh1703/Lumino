using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumino.Api.Application.Services
{
    public class VocabularyService : IVocabularyService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly IDateTimeProvider _dateTimeProvider;

        public VocabularyService(LuminoDbContext dbContext, IDateTimeProvider dateTimeProvider)
        {
            _dbContext = dbContext;
            _dateTimeProvider = dateTimeProvider;
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

            return query.ToList();
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

            return query.ToList();
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

            return new VocabularyResponse
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
            var translation = request.Translation.Trim();

            var item = _dbContext.VocabularyItems
                .FirstOrDefault(x => x.Word == word && x.Translation == translation);

            if (item == null)
            {
                item = new VocabularyItem
                {
                    Word = word,
                    Translation = translation,
                    Example = request.Example
                };

                _dbContext.VocabularyItems.Add(item);
                _dbContext.SaveChanges();
            }

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
                entity.NextReviewAt = now.AddHours(12);
            }

            _dbContext.SaveChanges();

            var item = _dbContext.VocabularyItems.First(x => x.Id == entity.VocabularyItemId);

            return new VocabularyResponse
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

        private static DateTime CalculateNextReviewAt(DateTime now, int reviewCount)
        {
            // Simplified spaced repetition (days): 1, 2, 4, 7, 14, 30, 60...
            var days =
                reviewCount switch
                {
                    1 => 1,
                    2 => 2,
                    3 => 4,
                    4 => 7,
                    5 => 14,
                    6 => 30,
                    _ => 60
                };

            return now.AddDays(days);
        }
    }
}
