using Calorie_Scanner_Tracker_And_Diet_Suggestor.Database;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Models;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.DTOs;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class MealsController : Controller
    {
        private readonly CalorieTrackerContext _context;
        private readonly NutritionApiService _nutritionApiService;
        private readonly ILogger<MealsController> _logger;

        public MealsController(
            CalorieTrackerContext context,
            NutritionApiService nutritionApiService,
            ILogger<MealsController> logger)
        {
            _context = context;
            _nutritionApiService = nutritionApiService;
            _logger = logger;
        }

        #region LIST & FILTER MEALS
        [HttpGet("")]
        public async Task<IActionResult> Index(
            string type = "breakfast",
            int page = 1,
            int pageSize = 10,
            int? minCalories = null,
            int? maxCalories = null,
            string? sortBy = null,
            string? searchTerm = null)
        {
            int userId = GetCurrentUserId();

            var query = _context.Meals.AsQueryable();

            // Filter by type (case-insensitive)
            if (!string.IsNullOrEmpty(type))
                query = query.Where(m => m.MealType.ToLower() == type.ToLower());

            // Filter by calories
            if (minCalories.HasValue)
                query = query.Where(m => m.Calories >= minCalories.Value);
            if (maxCalories.HasValue)
                query = query.Where(m => m.Calories <= maxCalories.Value);

            // Search by name (case-insensitive)
            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(m => m.Name.ToLower().Contains(searchTerm.ToLower()));

            // Sorting
            query = sortBy?.ToLower() switch
            {
                "calories" => query.OrderBy(m => m.Calories),
                "protein" => query.OrderBy(m => m.Protein),
                "carbs" => query.OrderBy(m => m.Carbs),
                "fats" => query.OrderBy(m => m.Fats),
                _ => query.OrderBy(m => m.Name),
            };

            // Pagination
            var meals = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Meals already eaten by this user
            var eatenMealIds = await _context.FoodLogs
                .Where(f => f.UserId == userId && f.IsEaten)
                .Select(f => f.MealId)
                .ToListAsync();

            ViewBag.MealType = type;
            ViewBag.EatenMealIds = eatenMealIds;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)await query.CountAsync() / pageSize);
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SortBy = sortBy;

            return View(meals);
        }
        #endregion

        #region TOGGLE EATEN STATUS
        [HttpPost("ToggleEaten")]
        public async Task<IActionResult> ToggleEaten(int mealId)
        {
            int userId = GetCurrentUserId();

            var mealExists = await _context.Meals.AnyAsync(m => m.Id == mealId);
            if (!mealExists) return NotFound("Meal not found.");

            var log = await _context.FoodLogs
                .FirstOrDefaultAsync(f => f.UserId == userId && f.MealId == mealId);

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

        #region MEAL CRUD
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
            if (meal == null) return NotFound();
            return View(meal);
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

        #region PREPARATION STEPS
        [HttpGet("PreparationSteps/{id}")]
        public async Task<IActionResult> PreparationSteps(int id)
        {
            var meal = await _context.Meals
                .Include(m => m.PreparationSteps)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (meal == null) return NotFound();
            return View(meal);
        }
        #endregion

        #region IMAGE ANALYSIS VIA AI
        [HttpPost("UploadImage")]
        public async Task<IActionResult> UploadImage([FromForm] string imageData, [FromForm] IFormFile imageFile)
        {
            if (string.IsNullOrEmpty(imageData) && (imageFile == null || imageFile.Length == 0))
                return BadRequest("No image data or file received.");

            try
            {
                byte[] imageBytes;
                string fileName;

                if (!string.IsNullOrEmpty(imageData))
                {
                    var base64Data = imageData.Contains(",") ? imageData.Split(',')[1] : imageData;
                    imageBytes = Convert.FromBase64String(base64Data);
                    fileName = $"{Guid.NewGuid()}.png";
                }
                else
                {
                    using var ms = new MemoryStream();
                    await imageFile.CopyToAsync(ms);
                    imageBytes = ms.ToArray();
                    fileName = Path.GetFileName(imageFile.FileName); // sanitize
                }

                // Validate file type
                var validExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(fileName).ToLowerInvariant();
                if (!validExtensions.Contains(ext)) return BadRequest("Unsupported image format.");

                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                var filePath = Path.Combine(uploadDir, fileName);
                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);
                var imageUrl = $"/uploads/{fileName}";

                // Call AI
                var analysis = await AnalyzeMealImageAsync(imageBytes, fileName);
                if (!string.IsNullOrEmpty(analysis.Error))
                    return StatusCode(400, new { message = analysis.Error, imageUrl });

                int userId = GetCurrentUserId();

                // Wrap meal + food log in transaction
                using var transaction = await _context.Database.BeginTransactionAsync();
                var meal = new Meals
                {
                    Name = "AI Detected Meal",
                    MealType = "Lunch",
                    Calories = analysis.Calories,
                    Protein = analysis.Protein,
                    Carbs = analysis.Carbs,
                    Fats = analysis.Fats,
                    ImageUrl = imageUrl
                };
                _context.Meals.Add(meal);
                await _context.SaveChangesAsync();

                var foodLog = new FoodLog
                {
                    UserId = userId,
                    MealId = meal.Id,
                    DateLogged = DateTime.UtcNow,
                    IsEaten = true,
                    Calories = meal.Calories,
                    Protein = meal.Protein,
                    Carbs = meal.Carbs,
                    Fats = meal.Fats,
                    ImageUrl = imageUrl
                };
                _context.FoodLogs.Add(foodLog);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Image uploaded and analyzed successfully.",
                    imageUrl = foodLog.ImageUrl,
                    nutrition = new
                    {
                        foodLog.Calories,
                        foodLog.Protein,
                        foodLog.Carbs,
                        foodLog.Fats
                    },
                    warning = analysis.WarningMessage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading/analyzing image.");
                return StatusCode(500, new { message = "Error processing image.", error = ex.Message });
            }
        }

        private async Task<FoodAnalysisResult> AnalyzeMealImageAsync(byte[] imageBytes, string fileName)
        {
            try
            {
                // Directly return what the service gives you
                return await _nutritionApiService.AnalyzeImageAsync(imageBytes, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI analysis failed");
                return new FoodAnalysisResult { Error = "AI analysis failed." };
            }
        }


        public class FoodAnalysisApiResponse
        {
            public decimal Calories { get; set; }
            public decimal Protein { get; set; }
            public decimal Carbs { get; set; }
            public decimal Fats { get; set; }
            public string? Error { get; set; }
            public string? Warning { get; set; }
        }
        #endregion

        #region UTILITY
        private int GetCurrentUserId()
        {
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                return userId;
            throw new UnauthorizedAccessException("User ID is invalid.");
        }
        #endregion

        public IActionResult Capture() => View();
    }
}