using Calorie_Scanner_Tracker_And_Diet_Suggestor.Database;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Models;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Controllers
{
    [Authorize]

    public class MealsController : Controller
    {
        private readonly CalorieTrackerContext _context;
        private readonly NutritionApiService _nutritionApiService;
        public MealsController(CalorieTrackerContext context, NutritionApiService nutritionApiService)
        {
            _context = context;
            _nutritionApiService = nutritionApiService;
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
        public async Task<IActionResult> GetMeal(int id)
        {
            Console.WriteLine($"GetMeal called with id={id}");  // or use ILogger
            var meal = await _context.Meals.Include(m => m.PreparationSteps).FirstOrDefaultAsync(m => m.Id == id);
            if (meal == null)
            {
                Console.WriteLine("Meal not found");
                return NotFound();
            }
            return Json(new
            {
                id = meal.Id,
                name = meal.Name,
                preparationSteps = meal.PreparationSteps
                    .OrderBy(s => s.StepNumber)
                    .Select(s => new { s.Id, s.StepNumber, s.Description })
            });
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
        public async Task<IActionResult> UploadImage([FromForm] string imageData, [FromForm] IFormFile imageFile)
        {
            if (string.IsNullOrEmpty(imageData) && (imageFile == null || imageFile.Length == 0))
                return BadRequest(new { message = "No image data or file received" });

            try
            {
                byte[] imageBytes;
                string fileName;

                if (!string.IsNullOrEmpty(imageData))
                {
                    // Process Base64 string (usually from camera)
                    var base64Data = imageData.Contains(",") ? imageData.Split(',')[1] : imageData;
                    imageBytes = Convert.FromBase64String(base64Data);
                    fileName = $"{Guid.NewGuid()}.png"; // Assuming PNG for camera captures
                }
                else
                {
                    // Process file upload (from device)
                    using (var ms = new MemoryStream())
                    {
                        await imageFile.CopyToAsync(ms);
                        imageBytes = ms.ToArray();
                    }
                    fileName = imageFile.FileName;
                }

                // Define storage path for the uploaded image
                var filePath = Path.Combine("wwwroot/uploads", fileName);
                var imageUrl = $"/uploads/{fileName}";

                // Ensure the uploads directory exists
                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                // Save image to server
                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                // Analyze image by calling the Python ML API
                var analysis = await AnalyzeMealImageAsync(imageBytes, fileName); // Pass bytes and filename

                // Check for errors or warnings from the Python API
                if (!string.IsNullOrEmpty(analysis.Error))
                {
                    // Propagate the specific error message from the Python API
                    return StatusCode(400, new { message = analysis.Error, imageUrl = imageUrl });
                }

                // Save analyzed meal to database (direct use of AI prediction)
                var meal = new Meals
                {
                    Name = "AI Analyzed Meal", // Default name, consider user input or specific food detection
                    Calories = analysis.Calories,
                    Protein = analysis.Protein,
                    Carbs = analysis.Carbs,
                    Fats = analysis.Fats,
                    MealType = "Lunch", // You might want to let the user select this, or infer later
                    ImageUrl = imageUrl
                };

                _context.Meals.Add(meal);
                await _context.SaveChangesAsync();

                // Log to FoodLog
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var foodLog = new FoodLog
                {
                    UserId = userId,
                    MealId = meal.Id,
                    DateLogged = DateTime.Now,
                    IsEaten = true,
                    Calories = meal.Calories,
                    Protein = meal.Protein,
                    Carbs = meal.Carbs,
                    Fats = meal.Fats,
                    ImageUrl = imageUrl
                };

                _context.FoodLogs.Add(foodLog);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Image uploaded and analyzed successfully",
                    imageUrl = foodLog.ImageUrl,
                    nutrition = new
                    {
                        foodLog.Calories,
                        foodLog.Protein,
                        foodLog.Carbs,
                        foodLog.Fats
                    },
                    warning = analysis.WarningMessage // Pass warning message to frontend
                });
            }
            catch (HttpRequestException httpEx)
            {
                // Handle network or API availability issues
                return StatusCode(503, new { message = "Nutrition analysis service is unavailable. Please try again later.", error = httpEx.Message });
            }
            catch (JsonException jsonEx)
            {
                // Handle issues with deserializing the API response
                return StatusCode(500, new { message = "Failed to parse analysis results. Service response format might be incorrect.", error = jsonEx.Message });
            }
            catch (Exception ex)
            {
                // Catch any other unexpected errors
                return StatusCode(500, new { message = "An unexpected error occurred during image processing.", error = ex.Message });
            }
        }

        // Updated signature to accept byte[] and fileName
        private async Task<FoodAnalysisResult> AnalyzeMealImageAsync(byte[] imageBytes, string fileName)
        {
            var jsonResponse = await _nutritionApiService.AnalyzeImageAsync(imageBytes, fileName);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var apiResult = JsonSerializer.Deserialize<FoodAnalysisApiResponse>(jsonResponse, options);

            if (apiResult == null)
            {
                // If deserialization results in null, it's a critical error or unexpected response
                return new FoodAnalysisResult { Error = "Failed to deserialize API response. Check API output.", Calories = 0, Protein = 0, Carbs = 0, Fats = 0 };
            }

            // Check if the API returned an error property
            if (!string.IsNullOrEmpty(apiResult.Error))
            {
                return new FoodAnalysisResult { Error = apiResult.Error, Calories = 0, Protein = 0, Carbs = 0, Fats = 0 };
            }

            // If you want to use the AI's direct prediction, don't use FindClosestMealId
            // If you still want to map to an existing meal, then keep it.
            // For now, I'm removing it to directly use the AI's values.
            // int mealId = FindClosestMealId(apiResult); // Removed for direct AI usage

            return new FoodAnalysisResult
            {
                // MealId = mealId, // Remove if you're not mapping to existing meals
                Calories = apiResult.Calories, // Directly use calories from API
                Protein = apiResult.Protein,
                Carbs = apiResult.Carbs,
                Fats = apiResult.Fats,
                WarningMessage = apiResult.Warning // Pass any warnings
            };
        }


        private int FindClosestMealId(FoodAnalysisApiResponse apiResult)
        {
            var meals = _context.Meals.ToList();
            var closestMeal = meals.OrderBy(m =>
                Math.Abs((decimal)m.Protein - apiResult.Protein) +
                Math.Abs((decimal)m.Carbs - apiResult.Carbs) +
                Math.Abs((decimal)m.Fats - apiResult.Fats)
            ).FirstOrDefault();

            return closestMeal?.Id ?? 1; // fallback to 1 if no match
        }


        private int CalculateCalories(FoodAnalysisApiResponse apiResult)
        {
            // Use standard macro formula: 4 cal/g (carbs, protein), 9 cal/g (fats)
            return (int)Math.Round(apiResult.Protein * 4 + apiResult.Carbs * 4 + apiResult.Fats * 9);
        }


        public class FoodAnalysisResult
        {
            public int MealId { get; set; } // May become optional if not mapping to existing meals
            public decimal Calories { get; set; } // Changed to double for precision
            public decimal Protein { get; set; }  // Changed to double for precision
            public decimal  Carbs { get; set; }    // Changed to double for precision
            public decimal Fats { get; set; }     // Changed to double for precision
            public string? Error { get; set; } //
            public string? WarningMessage { get; set; } // For backend warnings
        }

        public class FoodAnalysisApiResponse
        {
            public decimal Carbs { get; set; }
            public decimal Fats { get; set; }
            public decimal Protein { get; set; }
            public decimal Calories { get; set; } // Added Calories from Python API
            public string? Error { get; set; }   // For error messages from Python API
            public string? Warning { get; set; } // For warning messages from Python API
        }


        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        }

        public IActionResult Capture()
        {
            return View();
        }
    }
}
