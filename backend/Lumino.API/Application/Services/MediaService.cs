using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;

namespace Lumino.Api.Application.Services
{
    public class MediaService : IMediaService
    {
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        private static readonly string[] AllowedExtensions = new[]
        {
            ".png", ".jpg", ".jpeg", ".gif", ".webp",
            ".mp3", ".wav", ".ogg",
            ".glb", ".gltf",
            ".json"
        };

        public UploadMediaResponse Upload(IFormFile file, string baseUrl)
        {
            if (file == null)
            {
                throw new Exception("File is required");
            }

            if (file.Length <= 0)
            {
                throw new Exception("File is empty");
            }

            if (file.Length > MaxFileSizeBytes)
            {
                throw new Exception("File is too large");
            }

            var ext = Path.GetExtension(file.FileName).ToLower();

            if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            {
                throw new Exception("File format is not allowed");
            }

            var root = Directory.GetCurrentDirectory();
            var uploadsPath = Path.Combine(root, "wwwroot", "uploads");

            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            var storedFileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploadsPath, storedFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            return new UploadMediaResponse
            {
                Url = $"{baseUrl}/uploads/{storedFileName}",
                ContentType = file.ContentType,
                FileName = storedFileName
            };
        }
    }
}
