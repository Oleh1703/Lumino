using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Validators
{
    public interface ISubmitSceneRequestValidator
    {
        void Validate(SubmitSceneRequest request);
    }
}
