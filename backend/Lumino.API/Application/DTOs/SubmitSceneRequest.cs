using System.Collections.Generic;

namespace Lumino.Api.Application.DTOs
{
    public class SubmitSceneRequest
    {
        public string? IdempotencyKey { get; set; }

        public List<SubmitSceneAnswerRequest> Answers { get; set; } = new();
    }
}
