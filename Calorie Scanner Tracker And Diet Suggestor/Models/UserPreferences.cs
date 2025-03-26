namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Models
{
    public class UserPreferences
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TargetCalories { get; set; }
        public string DietaryRestrictions { get; set; }
        public string Allergies { get; set; }
        public string MealPreferences { get; set; }

        public Users User { get; set; }

        public UserPreferences()
        {
            
        }
    }
}
