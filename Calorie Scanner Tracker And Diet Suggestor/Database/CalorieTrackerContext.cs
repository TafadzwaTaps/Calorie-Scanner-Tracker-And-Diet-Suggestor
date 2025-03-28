using Calorie_Scanner_Tracker_And_Diet_Suggestor.Models;
using Microsoft.EntityFrameworkCore;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Database
{
    public class CalorieTrackerContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public CalorieTrackerContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public DbSet<Meals> Meals { get; set; }
        public DbSet<FoodLog> FoodLogs { get; set; }
        public DbSet<PreparationStep> PreparationSteps { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Models.User> User { get; set; }
        public DbSet<UserPreferences> UserPreferences { get; set; }
        public DbSet<UserSettings> UserSettings { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string connectionString = _configuration.GetConnectionString("CalorieTrackerConnection");
            optionsBuilder.UseSqlServer(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Meals>().HasKey(t => t.Id);
            modelBuilder.Entity<Models.User>().HasKey(u => u.Id);  // Ensure primary key
            modelBuilder.Entity<FoodLog>().HasKey(t => t.Id);
            modelBuilder.Entity<PreparationStep>().HasKey(t => t.Id);
            modelBuilder.Entity<UserPreferences>().HasKey(t => t.Id);
            modelBuilder.Entity<UserSettings>().HasKey(t => t.Id);
            modelBuilder.Entity<Message>().HasKey(t => t.MessageId);
        }
    }
}
