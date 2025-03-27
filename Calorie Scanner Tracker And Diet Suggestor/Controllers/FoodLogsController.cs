using Calorie_Scanner_Tracker_And_Diet_Suggestor.Database;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class FoodLogsController : Controller
    {
        private readonly CalorieTrackerContext _context;

        public FoodLogsController(CalorieTrackerContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Log authentication claims
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            Console.WriteLine($"[DEBUG] UserId Claim: {userIdClaim}");
            Console.WriteLine($"[DEBUG] User Role: {userRole}");
            Console.WriteLine($"[DEBUG] User Authenticated: {User.Identity.IsAuthenticated}");

            if (string.IsNullOrEmpty(userIdClaim))
            {
                Console.WriteLine("[ERROR] User is not authenticated!");
                return Unauthorized(); // Ensure the user is authenticated
            }

            int userId = int.Parse(userIdClaim); // Now it's safe to parse

            if (userRole == "Admin")
            {
                var foodLogs = await _context.FoodLogs
                    .Include(f => f.Meal)
                    .Include(f => f.User)
                    .ToListAsync();
                return View("AdminIndex", foodLogs);
            }
            else
            {
                var userLogs = await _context.FoodLogs
                    .Where(f => f.UserId == userId)
                    .Include(f => f.Meal)
                    .OrderByDescending(f => f.DateLogged)
                    .ToListAsync();
                return View("UserIndex", userLogs);
            }
        }




        [HttpGet]
        [Route("Create")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Users = new SelectList(await _context.Users.ToListAsync(), "Id", "Username");
            ViewBag.Meals = new SelectList(await _context.Meals.ToListAsync(), "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Create")]
        public async Task<IActionResult> Create(FoodLog foodLog)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            Console.WriteLine($"[DEBUG] Create Method - UserId: {userIdClaim}, Role: {userRole}");
            Console.WriteLine($"[DEBUG] User Authenticated: {User.Identity.IsAuthenticated}");

            if (string.IsNullOrEmpty(userIdClaim))
            {
                Console.WriteLine("[ERROR] Unauthorized attempt to create food log!");
                return RedirectToAction("Login", "Account"); // Redirect to login instead of returning 401
            }

            foodLog.UserId = int.Parse(userIdClaim);
            foodLog.DateLogged = DateTime.UtcNow;

            if (!ModelState.IsValid)
            {
                ViewBag.Meals = new SelectList(await _context.Meals.ToListAsync(), "Id", "Name");
                return View(foodLog);
            }

            _context.FoodLogs.Add(foodLog);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Dashboard));
        }




        [HttpPost]
        [Route("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var foodLog = await _context.FoodLogs.FindAsync(id);
            if (foodLog == null)
            {
                return NotFound();
            }

            _context.FoodLogs.Remove(foodLog);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("ByUser/{userId}")]
        public async Task<ActionResult<IEnumerable<FoodLog>>> GetFoodLogsByUser(int userId)
        {
            var logs = await _context.FoodLogs
                .Where(f => f.UserId == userId)
                .Include(f => f.Meal)
                .Include(f => f.User)
                .ToListAsync();

            return logs;
        }

        [HttpGet("ByDate/{date}")]
        public async Task<ActionResult<IEnumerable<FoodLog>>> GetFoodLogsByDate(DateTime date)
        {
            var logs = await _context.FoodLogs
                .Where(f => f.DateLogged.Date == date.Date)
                .Include(f => f.Meal)
                .Include(f => f.User)
                .ToListAsync();

            return logs;
        }

        [HttpGet("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            Console.WriteLine($"[DEBUG] Dashboard Access - UserId: {userIdClaim}, Role: {userRole}");
            Console.WriteLine($"[DEBUG] User Authenticated: {User.Identity.IsAuthenticated}");

            if (string.IsNullOrEmpty(userIdClaim))
            {
                Console.WriteLine("[ERROR] Unauthorized access to Dashboard!");
                return RedirectToAction("Login", "Account"); // Redirect to login instead of returning 401
            }

            int userId = int.Parse(userIdClaim);

            var logs = await _context.FoodLogs
                .Where(f => f.UserId == userId)
                .Include(f => f.Meal)
                .OrderByDescending(f => f.DateLogged)
                .Take(5)
                .ToListAsync();

            return View(logs);
        }

        public async Task<IActionResult> FoodLogs(int userId)
        {
            var foodLogs = await _context.FoodLogs
                .Where(f => f.UserId == userId)
                .Join(_context.Meals,
                    foodLog => foodLog.MealId,
                    meal => meal.Id,
                    (foodLog, meal) => new
                    {
                        foodLog.Id,
                        foodLog.DateLogged,
                        meal.Name,
                        meal.Calories,
                        meal.Protein,
                        meal.Carbs,
                        meal.Fats
                    })
                .ToListAsync();

            return View(foodLogs);
        }

        public async Task<IActionResult> SuggestMeals(int maxCalories, string dietaryRestrictions)
        {
            var meals = await _context.Meals
                .Where(m => m.Calories <= maxCalories && m.MealType.Contains(dietaryRestrictions))
                .ToListAsync();

            return View("Index", meals);
        }

        [HttpPost]
        [Route("CreateAjax")]
        public async Task<IActionResult> CreateAjax([FromBody] MealTrackerRequest mealData)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Json(new { success = false, message = "User not authenticated." });
            }

            int userId = int.Parse(userIdClaim);

            // Check if the meal exists
            var meal = await _context.Meals.FindAsync(mealData.MealId);
            if (meal == null)
            {
                return Json(new { success = false, message = "Meal not found." });
            }

            // Add meal to FoodLogs
            var foodLog = new FoodLog
            {
                MealId = meal.Id,
                UserId = userId,
                DateLogged = DateTime.UtcNow
            };

            _context.FoodLogs.Add(foodLog);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Meal added to tracker!" });
        }

        public class MealTrackerRequest
        {
            public int MealId { get; set; }
        }
    }
}
 