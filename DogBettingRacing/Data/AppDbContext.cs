using Microsoft.EntityFrameworkCore;
using DogBettingRacing.Models;

namespace DogBettingRacing.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Bet> Bets { get; set; }
        public DbSet<Round> Rounds { get; set; }
        public DbSet<Dog> Dogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Only configure the database if not done in Startup/Program.cs (for testing)
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=dogBettingRacing.db");
            }
        }
    }
}

