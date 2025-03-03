using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Database;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Models;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Controllers
{
    [Route("PreparationSteps")]
    public class PreparationStepsController : Controller
    {
        private readonly CalorieTrackerContext _context;

        public PreparationStepsController(CalorieTrackerContext context)
        {
            _context = context;
        }

        // 📌 GET: List Preparation Steps for a Meal
        [HttpGet("Index/{mealId}")]
        public async Task<IActionResult> Index(int mealId)
        {
            var meal = await _context.Meals
                .Include(m => m.PreparationSteps)
                .FirstOrDefaultAsync(m => m.Id == mealId);

            if (meal == null)
            {
                Console.WriteLine($"❌ Meal with ID {mealId} not found.");
                return NotFound();
            }

            Console.WriteLine($"✅ Meal found: {meal.Name} with {meal.PreparationSteps.Count} steps.");
            return View(meal);
        }

        // 📌 GET: Show Form to Add a Step
        [HttpGet("Create/{mealId}")]
        public IActionResult Create(int mealId)
        {
            var step = new PreparationStep { MealId = mealId }; // Pre-fill MealId
            ViewBag.MealId = mealId;
            return View(step);
        }

        // 📌 POST: Save New Preparation Step
        [HttpPost("Create/{mealId}")]
        public async Task<IActionResult> Create(int mealId, [Bind("MealId,StepNumber,Description")] PreparationStep step)
        {
            Console.WriteLine($"📌 Meal ID Received: {mealId}");

            // Ensure MealId is set
            step.MealId = mealId;

            if (!ModelState.IsValid)
            {
                Console.WriteLine("❌ ModelState is Invalid!");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"⚠️ Model Error: {error.ErrorMessage}");
                }

                ViewBag.MealId = mealId;
                return View(step);
            }

            // Save step to the database
            _context.PreparationSteps.Add(step);
            int changes = await _context.SaveChangesAsync();
            Console.WriteLine($"✅ Step saved! {changes} rows affected.");

            return RedirectToAction("Index", new { mealId = step.MealId });
        }


        // 📌 POST: Delete a Preparation Step
        [HttpPost("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var step = await _context.PreparationSteps.FindAsync(id);
            if (step == null)
            {
                Console.WriteLine($"❌ Step with ID {id} not found.");
                return NotFound();
            }

            _context.PreparationSteps.Remove(step);
            await _context.SaveChangesAsync();
            Console.WriteLine($"✅ Step {id} deleted successfully!");

            return RedirectToAction("Index", new { mealId = step.MealId });
        }
    }
}
