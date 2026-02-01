using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Validators
{
    public class SubmitLessonRequestValidator : ISubmitLessonRequestValidator
    {
        public void Validate(SubmitLessonRequest request)
        {
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            if (request.LessonId <= 0)
            {
                throw new ArgumentException("LessonId is invalid");
            }

            if (request.Answers == null || request.Answers.Count == 0)
            {
                throw new ArgumentException("Answers are required");
            }

            foreach (var answer in request.Answers)
            {
                if (answer == null)
                {
                    throw new ArgumentException("Answer item is invalid");
                }

                if (answer.ExerciseId <= 0)
                {
                    throw new ArgumentException("ExerciseId is invalid");
                }

                if (string.IsNullOrWhiteSpace(answer.Answer))
                {
                    throw new ArgumentException("Answer is required");
                }
            }
        }
    }
}
