using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Models
{
    public class Meals
    {
        public int Id { get; set; }
        [Required]
        public string? Name { get; set; }
        public decimal Calories { get; set; } // Change to double
        public decimal Protein { get; set; }  // Change to double
        public decimal Carbs { get; set; }    // Change to double
        public decimal Fats { get; set; }     // Change to double
        public string? MealType { get; set; } // Breakfast, Lunch, Dinner

        public List<PreparationStep> PreparationSteps { get; set; } = new List<PreparationStep>();

        [NotMapped]

        public string ImageUrl { get; set; }
        public Meals()
        {
            
        }
    }
}
