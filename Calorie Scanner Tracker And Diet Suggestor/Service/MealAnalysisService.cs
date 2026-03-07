using Calorie_Scanner_Tracker_And_Diet_Suggestor.Database;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.DTOs;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Models;
using Microsoft.EntityFrameworkCore;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Service
{
    public class MealAnalysisService
    {
        private readonly CalorieTrackerContext _context;
        private readonly ILogger<MealAnalysisService> _logger;

        public MealAnalysisService(
            CalorieTrackerContext context,
            ILogger<MealAnalysisService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogFoods(int userId, string imageUrl, FoodAnalysisResult result)
        {
            if (result == null)
                return;

            try
            {
                var meal = new Meals
                {
                    Name = result.FoodName ?? "Detected Meal",
                    Calories = (int)Math.Round(result.Calories),
                    Protein = (int)Math.Round(result.Protein),
                    Carbs = (int)Math.Round(result.Carbs),
                    Fats = (int)Math.Round(result.Fats),
                    MealType = "detected",
                    ImageUrl = imageUrl
                };

                _context.Meals.Add(meal);
                await _context.SaveChangesAsync();

                var log = new FoodLog
                {
                    UserId = userId,
                    MealId = meal.Id,
                    IsEaten = true,
                    DateLogged = DateTime.UtcNow
                };

                _context.FoodLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging analyzed meal");
            }
        }
    }
}