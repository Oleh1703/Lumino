namespace Lumino.Api.Application.Validators
{
    public interface ICourseStructureValidator
    {
        void ValidateOrThrow(int courseId);
    }
}
