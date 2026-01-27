namespace Lumino.Api.Domain.Entities
{
    public class Achievement
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public string Description { get; set; } = null!;
    }
}
