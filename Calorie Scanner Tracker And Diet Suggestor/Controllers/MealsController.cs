using Calorie_Scanner_Tracker_And_Diet_Suggestor.Database;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Models;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class MealsController : Controller
    {
        private readonly CalorieTrackerContext _context;
        private readonly NutritionApiService _nutritionApiService;
        private readonly MealAnalysisService _mealAnalysisService;
        private readonly ILogger<MealsController> _logger;

        public MealsController(
            CalorieTrackerContext context,
            NutritionApiService nutritionApiService,
            MealAnalysisService mealAnalysisService,
            ILogger<MealsController> logger)
        {
            _context = context;
            _nutritionApiService = nutritionApiService;
            _mealAnalysisService = mealAnalysisService;
            _logger = logger;
        }

        #region INDEX / LIST & FILTER
        [HttpGet("")]
        public async Task<IActionResult> Index(
            string type = "breakfast",
            int page = 1,
            int pageSize = 12,
            int? minCalories = null,
            int? maxCalories = null,
            string? sortBy = null,
            string? searchTerm = null)
        {
            int userId = GetCurrentUserId();
            var query = _context.Meals.AsQueryable();

            if (!string.IsNullOrEmpty(type))
                query = query.Where(m => m.MealType.ToLower() == type.ToLower());

            if (minCalories.HasValue)
                query = query.Where(m => m.Calories >= minCalories.Value);
            if (maxCalories.HasValue)
                query = query.Where(m => m.Calories <= maxCalories.Value);

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(m => m.Name.ToLower().Contains(searchTerm.ToLower()));

            query = sortBy?.ToLower() switch
            {
                "calories" => query.OrderBy(m => m.Calories),
                "protein" => query.OrderBy(m => m.Protein),
                "carbs" => query.OrderBy(m => m.Carbs),
                "fats" => query.OrderBy(m => m.Fats),
                _ => query.OrderBy(m => m.Name)
            };

            var meals = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var eatenIds = await _context.FoodLogs
                .Where(f => f.UserId == userId && f.IsEaten)
                .Select(f => f.MealId)
                .ToListAsync();

            ViewBag.MealType = type;
            ViewBag.EatenMealIds = eatenIds;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(await query.CountAsync() / (double)pageSize);
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SortBy = sortBy;

            return View(meals);
        }
        #endregion

        #region TOGGLE EATEN AJAX
        [HttpPost("ToggleEaten")]
        public async Task<IActionResult> ToggleEaten(int mealId)
        {
            int userId = GetCurrentUserId();
            var mealExists = await _context.Meals.AnyAsync(m => m.Id == mealId);
            if (!mealExists) return NotFound("Meal not found.");

            var log = await _context.FoodLogs.FirstOrDefaultAsync(f => f.UserId == userId && f.MealId == mealId);
            if (log == null)
            {
                log = new FoodLog
                {
                    UserId = userId,
                    MealId = mealId,
                    IsEaten = true,
                    DateLogged = DateTime.UtcNow
                };
                _context.FoodLogs.Add(log);
            }
            else
            {
                log.IsEaten = !log.IsEaten;
                log.DateLogged = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, eaten = log.IsEaten });
        }
        #endregion

        #region CRUD
        [HttpGet("Create")] public IActionResult Create() => View();

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Meals meal)
        {
            if (meal == null || string.IsNullOrEmpty(meal.Name) || meal.Calories <= 0)
                return BadRequest("Invalid meal data.");

            _context.Meals.Add(meal);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Meal created successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var meal = await _context.Meals.FindAsync(id);
            return meal == null ? NotFound() : View(meal);
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Meals meal)
        {
            if (id != meal.Id) return BadRequest();
            _context.Update(meal);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Meal updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var meal = await _context.Meals.FindAsync(id);
            if (meal == null) return NotFound();
            _context.Meals.Remove(meal);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Meal deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region AI IMAGE UPLOAD & ANALYSIS
        [HttpPost("UploadImage")]
        public async Task<IActionResult> UploadImage(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return BadRequest(new { success = false, message = "No image uploaded." });

            int userId = GetCurrentUserId();

            try
            {
                // Read image bytes
                byte[] imageBytes;
                using (var ms = new MemoryStream())
                {
                    await imageFile.CopyToAsync(ms);
                    imageBytes = ms.ToArray();
                }

                // Save image to server
                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
                var filePath = Path.Combine(uploadDir, fileName);
                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);
                var imageUrl = $"/uploads/{fileName}";

                // Call AI Nutrition API
                var aiResult = await _nutritionApiService.AnalyzeImageAsync(imageBytes, imageFile.FileName);
                if (!string.IsNullOrEmpty(aiResult.Error))
                    return StatusCode(500, new { success = false, message = aiResult.Error, imageUrl });

                // Log foods detected
                await _mealAnalysisService.LogFoods(userId, imageUrl, aiResult);

                return Ok(new
                {
                    success = true,
                    message = "Image analyzed successfully",
                    imageUrl,
                    nutrition = new
                    {
                        calories = aiResult.Calories,
                        protein = aiResult.Protein,
                        carbs = aiResult.Carbs,
                        fats = aiResult.Fats
                    },
                    warning = aiResult.WarningMessage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing meal image");
                return StatusCode(500, new { success = false, message = "Error processing image.", error = ex.Message });
            }
        }
        #endregion

        #region UTILITY
        private int GetCurrentUserId()
        {
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                return userId;
            throw new UnauthorizedAccessException("User ID is invalid.");
        }

        [HttpGet("Capture")]
        public IActionResult Capture() => View();
        #endregion
    }
}