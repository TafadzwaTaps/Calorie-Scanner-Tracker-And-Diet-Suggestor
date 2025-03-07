using System.ComponentModel.DataAnnotations;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Models
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string? Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; } = string.Empty;

        public ResetPasswordViewModel()
        {

        }
    }
}
