using System.Threading.Tasks;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Service
{
    public interface IEmailService
    {
        Task SendMealReminder(string email, string mealName, string mealType);
    }
}
