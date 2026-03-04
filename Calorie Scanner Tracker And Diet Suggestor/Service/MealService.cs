using Calorie_Scanner_Tracker_And_Diet_Suggestor.Database;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Service
{
    public class MealService
    {
        private readonly CalorieTrackerContext _context;
        private readonly NutritionApiService _nutritionApiService;

        public MealService(CalorieTrackerContext context, NutritionApiService nutritionApiService)
        {
            _context = context;
            _nutritionApiService = nutritionApiService;
        }

        public async Task<List<Meals>> GetMealsAsync(string? type = null, int? minCalories = null,
            int? maxCalories = null, string? searchTerm = null)
        {
            var query = _context.Meals.AsQueryable();

            if (!string.IsNullOrEmpty(type)) query = query.Where(m => m.MealType.ToLower() == type.ToLower());
            if (minCalories.HasValue) query = query.Where(m => m.Calories >= minCalories);
            if (maxCalories.HasValue) query = query.Where(m => m.Calories <= maxCalories);
            if (!string.IsNullOrEmpty(searchTerm)) query = query.Where(m => m.Name.Contains(searchTerm));

            return await query.Select(m => new Meals
            {
                Id = m.Id,
                Name = m.Name,
                Calories = m.Calories,
                Protein = m.Protein,
                Carbs = m.Carbs,
                Fats = m.Fats,
                MealType = m.MealType,
                ImageUrl = m.ImageUrl
            }).ToListAsync();
        }

        public async Task<Meals?> UploadAndAnalyzeMealAsync(byte[] imageBytes, string fileName, int userId)
        {
            var analysis = await _nutritionApiService.AnalyzeImageAsync(imageBytes, fileName);
            if (!string.IsNullOrEmpty(analysis.Error)) return null;

            string uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);
            string filePath = Path.Combine(uploadDir, fileName);
            await File.WriteAllBytesAsync(filePath, imageBytes);

            var meal = new Meals
            {
                Name = "AI Analyzed Meal",
                Calories = analysis.Calories,
                Protein = analysis.Protein,
                Carbs = analysis.Carbs,
                Fats = analysis.Fats,
                MealType = "Lunch",
                ImageUrl = $"/uploads/{fileName}"
            };

            _context.Meals.Add(meal);
            await _context.SaveChangesAsync();

            _context.FoodLogs.Add(new FoodLog
            {
                UserId = userId,
                MealId = meal.Id,
                DateLogged = DateTime.UtcNow,
                IsEaten = true,
                Calories = meal.Calories,
                Protein = meal.Protein,
                Carbs = meal.Carbs,
                Fats = meal.Fats,
                ImageUrl = meal.ImageUrl
            });

            await _context.SaveChangesAsync();

            return new Meals
            {
                Id = meal.Id,
                Name = meal.Name,
                Calories = meal.Calories,
                Protein = meal.Protein,
                Carbs = meal.Carbs,
                Fats = meal.Fats,
                MealType = meal.MealType,
                ImageUrl = meal.ImageUrl
            };
        }
    }
}