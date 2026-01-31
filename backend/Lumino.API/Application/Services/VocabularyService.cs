using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumino.Api.Application.Services
{
    public class VocabularyService : IVocabularyService
    {
        private readonly LuminoDbContext _dbContext;

        public VocabularyService(LuminoDbContext dbContext)
        {
            _dbContext = dbContext;
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
                    LastReviewedAt = uv.LastReviewedAt
                };

            return query.ToList();
        }

        public void AddWord(int userId, AddVocabularyRequest request)
        {
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

            var userWord = new UserVocabulary
            {
                UserId = userId,
                VocabularyItemId = item.Id,
                AddedAt = DateTime.UtcNow,
                LastReviewedAt = null
            };

            _dbContext.UserVocabularies.Add(userWord);
            _dbContext.SaveChanges();
        }

        public void DeleteWord(int userId, int userVocabularyId)
        {
            var entity = _dbContext.UserVocabularies
                .First(x => x.Id == userVocabularyId && x.UserId == userId);

            _dbContext.UserVocabularies.Remove(entity);
            _dbContext.SaveChanges();
        }
    }
}
