using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Validators
{
    public class SubmitSceneRequestValidator : ISubmitSceneRequestValidator
    {
        public void Validate(SubmitSceneRequest request)
        {
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            if (request.Answers == null)
            {
                throw new ArgumentException("Answers are required");
            }

            foreach (var answer in request.Answers)
            {
                if (answer == null)
                {
                    throw new ArgumentException("Answer item is invalid");
                }

                if (answer.StepId <= 0)
                {
                    throw new ArgumentException("StepId is invalid");
                }

                if (string.IsNullOrWhiteSpace(answer.Answer))
                {
                    throw new ArgumentException("Answer is required");
                }
            }
        }
    }
}
