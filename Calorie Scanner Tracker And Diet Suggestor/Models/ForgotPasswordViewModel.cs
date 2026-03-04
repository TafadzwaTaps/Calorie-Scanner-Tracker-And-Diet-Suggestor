using System.ComponentModel.DataAnnotations;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Models
{
    public class ForgotPasswordViewModel
    {
        [Required]
        public string? Email { get; set; }

        public ForgotPasswordViewModel()
        {

        }
    }
}
