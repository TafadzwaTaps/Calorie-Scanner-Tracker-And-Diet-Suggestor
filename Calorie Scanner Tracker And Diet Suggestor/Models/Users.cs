using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Models
{
    public class Users
    {
        public int Id { get; set; }
        [Required]
        public string? Username { get; set; }
        [Required]
        public string? Email { get; set; }
        [NotMapped]
        public string Password { get; set; }
        public string? PasswordHash { get; set; }
        public List<FoodLog>? FoodLogs { get; set; }

        public Users()
        {
            
        }
    }
}
