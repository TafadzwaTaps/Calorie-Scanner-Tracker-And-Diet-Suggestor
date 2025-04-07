using Calorie_Scanner_Tracker_And_Diet_Suggestor.Database;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Controllers
{
    [Authorize]
    public class MealsController : Controller
    {
        private readonly CalorieTrackerContext _context;

        public MealsController(CalorieTrackerContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string type = "breakfast",
    int? page = 1,
    int pageSize = 10,
    int? minCalories = null,
    int? maxCalories = null,
    string? sortBy = null,
    string? searchTerm = null)

        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(); // Handle unauthorized if userId is missing.

            // Convert userId to integer (assuming it's stored as a string in Claims)
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized(); // Handle invalid userId format.
            }

            var query = _context.Meals.AsQueryable();

            // Filter by type
            if (!string.IsNullOrEmpty(type))
                query = query.Where(m => m.MealType.ToLower() == type.ToLower());

            // Filter by calories
            if (minCalories.HasValue)
                query = query.Where(m => m.Calories >= minCalories);
            if (maxCalories.HasValue)
                query = query.Where(m => m.Calories <= maxCalories);

            // Search
            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(m => m.Name.Contains(searchTerm));

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
            var meals = await query.Skip(((page ?? 1) - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();

            // Get Eaten Meals
            var eatenMealIds = await _context.FoodLogs
                .Where(f => f.UserId == userId && f.IsEaten) // Compare as integers
                .Select(f => f.MealId)
                .ToListAsync();

            ViewBag.MealType = type;
            ViewBag.EatenMealIds = eatenMealIds;

            return View(meals);
        }



        [HttpPost]
        public async Task<IActionResult> ToggleEaten(int mealId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

            // Convert userId to an integer
            if (!int.TryParse(userIdString, out var userId))
            {
                return BadRequest("Invalid user ID.");
            }

            var existingLog = await _context.FoodLogs
                .FirstOrDefaultAsync(f => f.UserId == userId && f.MealId == mealId);

            if (existingLog == null)
            {
                // Create a new food log if it doesn't exist
                _context.FoodLogs.Add(new FoodLog
                {
                    UserId = userId,
                    MealId = mealId,
                    IsEaten = true, // Set IsEaten to true
                    DateLogged = DateTime.UtcNow
                });
            }
            else
            {
                // Toggle the IsEaten status if the log already exists
                existingLog.IsEaten = !existingLog.IsEaten;
                existingLog.DateLogged = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpGet("SearchMeals")]
        public async Task<IActionResult> SearchMeals([FromQuery] string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return BadRequest("Search term cannot be empty.");
            }

            var meals = await _context.Meals
                .Where(m => m.Name.Contains(searchTerm))
                .ToListAsync();

            return View("Index", meals);
        }

        [HttpGet]
        public IActionResult PostMeal()
        {
            return View();
        }

        // Add a new meal to the database
        [HttpPost]
        public async Task<IActionResult> PostMeal(Meals meal)
        {
            if (meal == null || string.IsNullOrEmpty(meal.Name) || meal.Calories <= 0)
            {
                return BadRequest("Meal data is invalid. Please provide a valid name and calorie count.");
            }

            _context.Meals.Add(meal);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Meal added successfully!";
            return RedirectToAction(nameof(Index)); // Redirect to the list of meals
        }


        // Update an existing meal in the database
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMeal(int id, Meals meal)
        {
            if (id != meal.Id) return BadRequest();

            // Validate meal data
            if (meal == null || string.IsNullOrEmpty(meal.Name) || meal.Calories <= 0)
            {
                return BadRequest("Meal data is invalid.");
            }

            _context.Entry(meal).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Delete a meal by ID
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMeal(int id)
        {
            var meal = await _context.Meals.FindAsync(id);
            if (meal == null) return NotFound();
            _context.Meals.Remove(meal);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        public async Task<IActionResult> PreparationSteps(int id)
        {
            var meal = await _context.Meals.FindAsync(id);
            if (meal == null) return NotFound();

            return View(meal);
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage([FromForm] string imageData)
        {
            if (string.IsNullOrEmpty(imageData))
                return BadRequest(new { message = "No image data received" });

            try
            {
                // Convert Base64 string to byte array
                var base64Data = imageData.Split(',')[1];
                var imageBytes = Convert.FromBase64String(base64Data);

                // Define storage path
                var filePath = Path.Combine("wwwroot/uploads", $"{Guid.NewGuid()}.png");

                // Save image to server
                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                return Ok(new { message = "Image uploaded successfully", filePath });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error uploading image", error = ex.Message });
            }
        }
        public IActionResult Capture()
        {
            return View();
        }
    }
}
