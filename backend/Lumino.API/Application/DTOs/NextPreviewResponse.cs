using System;

namespace Lumino.Api.Application.DTOs
{
    public class NextPreviewResponse
    {
        public NextActivityResponse? Next { get; set; }

        public UserProgressResponse Progress { get; set; } = new UserProgressResponse();

        public DateTime GeneratedAt { get; set; }
    }
}
