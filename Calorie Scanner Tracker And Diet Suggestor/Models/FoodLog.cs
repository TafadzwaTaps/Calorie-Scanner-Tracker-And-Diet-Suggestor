namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Models
{
    public class FoodLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public int MealId { get; set; }
        public Meals? Meal { get; set; }
        public DateTime DateLogged { get; set; }
        public bool IsEaten { get; set; } = false;
        public FoodLog()
        {
            
        }
    }
}
