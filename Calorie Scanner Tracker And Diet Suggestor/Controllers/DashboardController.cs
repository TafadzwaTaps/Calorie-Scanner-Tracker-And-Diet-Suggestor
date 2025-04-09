using Calorie_Scanner_Tracker_And_Diet_Suggestor.Database;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Controllers
{
    public class DashboardController : Controller
    {
        private readonly CalorieTrackerContext _context;

        public DashboardController(CalorieTrackerContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string mealType = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var userId = 1; // Example: Get from logged-in user (Session or Identity)

            // Set default date range to the last 7 days if no date range is provided
            startDate ??= DateTime.Today.AddDays(-7);
            endDate ??= DateTime.Today;

            // Filter food logs based on meal type and date range
            var foodLogsQuery = _context.FoodLogs
                .Include(f => f.Meal)
                .Where(f => f.UserId == userId && f.DateLogged.Date >= startDate && f.DateLogged.Date <= endDate);

            // Apply meal type filter if provided
            if (!string.IsNullOrEmpty(mealType))
            {
                foodLogsQuery = foodLogsQuery.Where(f => f.Meal.MealType.ToLower() == mealType.ToLower());
            }

            var foodLogs = await foodLogsQuery.ToListAsync();

            var totalCalories = foodLogs.Sum(f => f.Meal.Calories);
            var totalProtein = foodLogs.Sum(f => f.Meal.Protein);
            var totalCarbs = foodLogs.Sum(f => f.Meal.Carbs);
            var totalFats = foodLogs.Sum(f => f.Meal.Fats);

            // Grouping data by date for chart
            var dailyCalories = foodLogs
                .GroupBy(f => f.DateLogged.Date)
                .Select(g => new ChartDataItem
                {
                    Date = g.Key.ToString("MM/dd/yyyy"),
                    Calories = g.Sum(f => f.Meal.Calories)
                })
                .ToList();

            // Calculate macronutrient breakdown for Pie chart
            var pieChartData = new[] {
                totalProtein,  // Protein
                totalCarbs,    // Carbs
                totalFats      // Fats
            };

            var pieChartLabels = new[] { "Protein", "Carbs", "Fats" };  // Labels for Pie chart

            var dashboardViewModel = new DashboardViewModel
            {
                TotalCalories = totalCalories,
                TotalProtein = totalProtein,
                TotalCarbs = totalCarbs,
                TotalFats = totalFats,
                FoodLogs = foodLogs,
                ChartData = dailyCalories,
                PieChartData = pieChartData,  // Add Pie chart data to the view model
                PieChartLabels = pieChartLabels,  // Add Pie chart labels
                MealTypeFilter = mealType,
                StartDateFilter = startDate,
                EndDateFilter = endDate
            };

            return View(dashboardViewModel);
        }
    }
}
