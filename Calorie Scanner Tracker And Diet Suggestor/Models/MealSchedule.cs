namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Models
{
    public class MealSchedule
    {
        public int Id { get; set; }
        public int UserId { get; set; }  // Reference to the user
        public int MealId { get; set; }  // Reference to the meal
        public DateTime ScheduledDate { get; set; }
        public string Status { get; set; } = "Planned"; // "Planned", "Eaten", "Skipped"

        // Navigation properties
        public User User { get; set; }
        public Meals Meal { get; set; }

        public MealSchedule()
        {
            
        }
    }
}
