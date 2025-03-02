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

        private static readonly Dictionary<string, List<string>> meals = new()
        {
            { "breakfast", new List<string> { "Oatmeal", "Scrambled Eggs", "Fruit Smoothie" } },
            { "lunch", new List<string> { "Grilled Chicken Salad", "Quinoa Bowl", "Pasta" } },
            { "dinner", new List<string> { "Steak with Vegetables", "Salmon & Rice", "Vegetable Stir-Fry" } }
        };

        [HttpGet]
        [Route("Meals")]
        public IActionResult Index([FromQuery] string type = "breakfast")
        {
            if (string.IsNullOrEmpty(type) || !meals.ContainsKey(type.ToLower()))
            {
                return NotFound("Meal type not found. Use 'breakfast', 'lunch', or 'dinner'.");
            }

            ViewData["MealType"] = type;
            ViewData["Meals"] = meals[type.ToLower()];
            return View();
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
