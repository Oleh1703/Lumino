using Lumino.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lumino.Api.Data
{
    public class LuminoDbContext : DbContext
    {
        public LuminoDbContext(DbContextOptions<LuminoDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Course> Courses => Set<Course>();
        public DbSet<Topic> Topics => Set<Topic>();
        public DbSet<Lesson> Lessons => Set<Lesson>();
        public DbSet<Exercise> Exercises => Set<Exercise>();
    }
}
