using Google.Apis.Drive.v3.Data;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Models
{
    public class UserSettings
    {
        public int Id { get; set; }
        public int UserId { get; set; } // Foreign Key
        public User User { get; set; } // Navigation Property

        public string? Profile { get; set; }  // e.g., "Male, 4 Feb 2000, 174 cm"
        public string? Goals { get; set; }  // e.g., "90kg → 78kg, 0.5 kg/wk"
        public int DailyCalorieTarget { get; set; }
        public string? MealSchedule { get; set; }  // e.g., "Breakfast, Lunch, Dinner"
        public bool MealReminders { get; set; }
        public string? Language { get; set; }  // e.g., "English, UK"
        public int WaterGlassCapacity { get; set; }  // e.g., "200ml"
        public string? MenuDisplaySettings { get; set; }  // e.g., "P, F, C"
        public string? NavigationAppearance { get; set; }  // e.g., "Recipes, Measurements"
        public string? Theme { get; set; }  // e.g., "Dark, Light"

        public UserSettings()
        {
            
        }
    }
}
