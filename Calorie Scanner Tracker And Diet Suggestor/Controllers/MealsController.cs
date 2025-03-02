using Calorie_Scanner_Tracker_And_Diet_Suggestor.Database;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Controllers
{

    public class MealsController : Controller
    {
        private readonly CalorieTrackerContext _context;

        public MealsController(CalorieTrackerContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("Meals")]
        public async Task<IActionResult> Index([FromQuery] string type = "breakfast")
        {
            if (string.IsNullOrEmpty(type))
            {
                return NotFound("Meal type not found. Use 'breakfast', 'lunch', or 'dinner'.");
            }

            // Fetch meals from database based on type
            var meals = await _context.Meals
                .Where(m => m.MealType.ToLower() == type.ToLower())
                .ToListAsync();

            if (meals == null || !meals.Any())
            {
                return NotFound($"No meals found for {type}.");
            }

            ViewData["MealType"] = type;
            return View(meals); // Pass the meals list to the view
        }


        public IActionResult Breakfast()
        {
            return View();
        }

        public IActionResult Lunch()
        {
            return View();
        }

        public IActionResult Dinner()
        {
            return View();
        }

        public IActionResult Meals()
        {
            return View();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Meals>>> GetMeals()
        {
            return await _context.Meals.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Meals>> GetMeal(int id)
        {
            var meal = await _context.Meals.FindAsync(id);
            if (meal == null) return NotFound();
            return meal;
        }

        [HttpPost]
        public async Task<ActionResult<Meals>> PostMeal(Meals meal)
        {
            _context.Meals.Add(meal);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetMeal", new { id = meal.Id }, meal);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutMeal(int id, Meals meal)
        {
            if (id != meal.Id) return BadRequest();
            _context.Entry(meal).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMeal(int id)
        {
            var meal = await _context.Meals.FindAsync(id);
            if (meal == null) return NotFound();
            _context.Meals.Remove(meal);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
