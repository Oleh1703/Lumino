using Lumino.Api.Application.DTOs;
using System.Collections.Generic;

namespace Lumino.Api.Application.Interfaces
{
    public interface ITopicService
    {
        List<TopicResponse> GetTopicsByCourse(int courseId);
    }
}
