using Lumino.Api.Application.Services;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Lumino.Tests;

public class MediaServiceTests
{
    [Fact]
    public void Upload_NullFile_ShouldThrow()
    {
        var service = new MediaService();

        var ex = Assert.Throws<ArgumentException>(() =>
        {
            service.Upload(null!, "http://localhost");
        });

        Assert.Equal("File is required", ex.Message);
    }

    [Fact]
    public void Upload_EmptyFile_ShouldThrow()
    {
        var service = new MediaService();

        using var ms = new MemoryStream(Array.Empty<byte>());

        var file = new FormFile(ms, 0, ms.Length, "file", "test.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        var ex = Assert.Throws<ArgumentException>(() =>
        {
            service.Upload(file, "http://localhost");
        });

        Assert.Equal("File is empty", ex.Message);
    }

    [Fact]
    public void Upload_TooLarge_ShouldThrow()
    {
        var service = new MediaService();

        var bytes = new byte[10 * 1024 * 1024 + 1]; // 10MB + 1
        using var ms = new MemoryStream(bytes);

        var file = new FormFile(ms, 0, ms.Length, "file", "test.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        var ex = Assert.Throws<ArgumentException>(() =>
        {
            service.Upload(file, "http://localhost");
        });

        Assert.Equal("File is too large", ex.Message);
    }

    [Fact]
    public void Upload_NotAllowedExtension_ShouldThrow()
    {
        var service = new MediaService();

        using var ms = new MemoryStream(new byte[] { 1, 2, 3 });

        var file = new FormFile(ms, 0, ms.Length, "file", "virus.exe")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream"
        };

        var ex = Assert.Throws<ArgumentException>(() =>
        {
            service.Upload(file, "http://localhost");
        });

        Assert.Equal("File format is not allowed", ex.Message);
    }

    [Fact]
    public void Upload_Valid_ShouldSaveFile_AndReturnUrl()
    {
        var service = new MediaService();

        var originalDir = Directory.GetCurrentDirectory();

        var tempRoot = Path.Combine(Path.GetTempPath(), "LuminoMediaTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            Directory.SetCurrentDirectory(tempRoot);

            var bytes = new byte[] { 10, 20, 30, 40, 50 };
            using var ms = new MemoryStream(bytes);

            var file = new FormFile(ms, 0, ms.Length, "file", "image.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };

            var baseUrl = "http://localhost";

            var response = service.Upload(file, baseUrl);

            Assert.False(string.IsNullOrWhiteSpace(response.Url));
            Assert.False(string.IsNullOrWhiteSpace(response.FileName));
            Assert.Equal("image/png", response.ContentType);

            Assert.StartsWith($"{baseUrl}/uploads/", response.Url);
            Assert.EndsWith(".png", response.FileName.ToLower());

            var savedPath = Path.Combine(tempRoot, "wwwroot", "uploads", response.FileName);
            Assert.True(File.Exists(savedPath));

            var savedBytes = File.ReadAllBytes(savedPath);
            Assert.Equal(bytes, savedBytes);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);

            try
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
            }
            catch
            {
                // cleanup intentionally ignored
            }
        }
    }
}
