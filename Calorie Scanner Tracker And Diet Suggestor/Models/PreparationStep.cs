using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Models
{
    public class PreparationStep
    {
        public int Id { get; set; }

        [Required]
        public int MealId { get; set; } // Foreign key

        [Required]
        public int StepNumber { get; set; } // Order of steps

        [Required]
        public string? Description { get; set; } // Actual step description

        [ForeignKey("MealId")]
        public Meals? Meal { get; set; } // Navigation property (Not required for form submission)

        public PreparationStep()
        {
            
        }
    }
}
