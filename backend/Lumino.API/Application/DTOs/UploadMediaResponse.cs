namespace Lumino.Api.Application.DTOs
{
    public class UploadMediaResponse
    {
        public string Url { get; set; } = null!;

        public string ContentType { get; set; } = null!;

        public string FileName { get; set; } = null!;
    }
}
