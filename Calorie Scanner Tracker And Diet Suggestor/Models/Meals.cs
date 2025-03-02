using System.ComponentModel.DataAnnotations;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Models
{
    public class Meals
    {
        public int Id { get; set; }
        [Required]
        public string? Name { get; set; }
        public int Calories { get; set; }
        public double Protein { get; set; }
        public double Carbs { get; set; }
        public double Fats { get; set; }
        public string? MealType { get; set; } // Breakfast, Lunch, Dinner
        public string? DietaryRestrictions { get; set; } // Vegan, Gluten-Free, etc.

        public Meals()
        {
            
        }
    }
}
