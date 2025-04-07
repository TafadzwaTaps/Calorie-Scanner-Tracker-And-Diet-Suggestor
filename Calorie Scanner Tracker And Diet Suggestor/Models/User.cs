using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Models
{
    public class User
    {
        public int Id { get; set; }
        [Required]
        public string? Username { get; set; }
        [Required]
        public string? Email { get; set; }
        [NotMapped]
        public string Password { get; set; }
        public string? PasswordHash { get; set; }

        [Required]
        public string Role { get; set; } = "User"; // Default role
        public List<FoodLog>? FoodLogs { get; set; }
        public UserPreferences? UserPreferences { get; set; }
        public UserSettings? UserSettings { get; set; }
       
        [NotMapped]              
        public bool EmailNotifications { get; set; }
        [NotMapped]
        public bool PushNotifications { get; set; }
        public User()
        {
            
        }
    }
}
