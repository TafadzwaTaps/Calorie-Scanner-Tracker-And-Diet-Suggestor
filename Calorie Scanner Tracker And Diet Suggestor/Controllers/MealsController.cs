using Calorie_Scanner_Tracker_And_Diet_Suggestor.Database;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Linq;
using System.Threading.Tasks;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Controllers
{
    public class MealsController : Controller
    {
        private readonly CalorieTrackerContext _context;

        public MealsController(CalorieTrackerContext context)
        {
            _context = context;
        }

        // Index Action - Fetch meals by meal type
        [HttpGet]
        [Route("Meals")]
        public async Task<IActionResult> Index([FromQuery] string type = "breakfast", [FromQuery] int? page = 1, [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrEmpty(type))
            {
                return NotFound("Meal type not found. Use 'breakfast', 'lunch', or 'dinner'.");
            }

            var mealsQuery = _context.Meals.Where(m => m.MealType.ToLower() == type.ToLower());

            // Apply pagination
            var meals = await mealsQuery.Skip((page.Value - 1) * pageSize).Take(pageSize).ToListAsync();

            if (meals == null || !meals.Any())
            {
                return NotFound($"No meals found for {type}.");
            }

            ViewData["MealType"] = type;
            return View(meals);
        }

        // Filtering meals by calories
        [HttpGet("FilterByCalories")]
        public async Task<IActionResult> FilterByCalories([FromQuery] int minCalories, [FromQuery] int maxCalories)
        {
            var meals = await _context.Meals
                .Where(m => m.Calories >= minCalories && m.Calories <= maxCalories)
                .ToListAsync();

            return View("Index", meals); // Return filtered meals to the Index view
        }

        // Sorting meals by calories, protein, carbs, or fats
        [HttpGet("SortMeals")]
        public async Task<IActionResult> SortMeals([FromQuery] string sortBy = "Calories", [FromQuery] string order = "asc")
        {
            var mealsQuery = _context.Meals.AsQueryable();

            switch (sortBy.ToLower())
            {
                case "calories":
                    mealsQuery = order == "asc" ? mealsQuery.OrderBy(m => m.Calories) : mealsQuery.OrderByDescending(m => m.Calories);
                    break;
                case "protein":
                    mealsQuery = order == "asc" ? mealsQuery.OrderBy(m => m.Protein) : mealsQuery.OrderByDescending(m => m.Protein);
                    break;
                case "carbs":
                    mealsQuery = order == "asc" ? mealsQuery.OrderBy(m => m.Carbs) : mealsQuery.OrderByDescending(m => m.Carbs);
                    break;
                case "fats":
                    mealsQuery = order == "asc" ? mealsQuery.OrderBy(m => m.Fats) : mealsQuery.OrderByDescending(m => m.Fats);
                    break;
                default:
                    mealsQuery = mealsQuery.OrderBy(m => m.Calories);
                    break;
            }

            var sortedMeals = await mealsQuery.ToListAsync();

            return View("Index", sortedMeals);
        }

        // Search for meals by name or ingredients
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

        // Suggest meals based on dietary preferences (e.g., low calorie meals)
        [HttpGet("SuggestMeals")]
        public async Task<IActionResult> SuggestMeals([FromQuery] int maxCalories)
        {
            var meals = await _context.Meals
                .Where(m => m.Calories <= maxCalories)
                .ToListAsync();

            return View("Index", meals);
        }

        // Get total count of meals for a specific meal type (e.g., breakfast, lunch, or dinner)
        [HttpGet("MealCount")]
        public async Task<IActionResult> MealCount([FromQuery] string type)
        {
            var count = await _context.Meals
                .Where(m => m.MealType.ToLower() == type.ToLower())
                .CountAsync();

            return Ok(new { Count = count });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMeal(int id)
        {
            var meal = await _context.Meals.FindAsync(id);
            if (meal == null) return NotFound();
            return Ok(new
            {
                meal.Id,
                meal.Name,
                meal.Calories,
                meal.Protein,
                meal.Carbs,
                meal.Fats,
                meal.MealType,
                meal.PreparationSteps // Include steps
            });
        }

        [HttpGet]

        public async Task<ActionResult<Meals>> PostMeal()
        {
            return View();
        }

        // Add a new meal to the database
        [HttpPost]
        public async Task<ActionResult<Meals>> PostMeal(Meals meal)
        {
            _context.Meals.Add(meal);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index)); // Redirect to the list of meals
        }

        // Update an existing meal in the database
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMeal(int id, Meals meal)
        {
            if (id != meal.Id) return BadRequest();
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

        public IActionResult Meals()
        {
            return View();
        }
    }
}
