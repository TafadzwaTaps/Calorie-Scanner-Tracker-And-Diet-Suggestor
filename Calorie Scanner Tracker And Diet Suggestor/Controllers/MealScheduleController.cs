using Calorie_Scanner_Tracker_And_Diet_Suggestor.Database;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Service;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Controllers
{
    [Route("[controller]")]
    public class MealScheduleController : Controller
    {
        private readonly CalorieTrackerContext _context;
        private readonly IEmailService _emailService;

        public MealScheduleController(CalorieTrackerContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            int userId = int.Parse(userIdClaim);

            var meals = await _context.MealSchedules
                .Where(m => m.UserId == userId)
                .Include(m => m.Meal)
                .ToListAsync();

            ViewBag.Meals = await _context.Meals.ToListAsync(); // 🟢 Fix: ViewBag for dropdown
            return View(meals ?? new List<MealSchedule>());     // 🟢 Ensure not null
        }

        [HttpGet("Schedule")]
        public IActionResult Schedule() => View();

        [HttpGet("UserMeals")]
        public async Task<IActionResult> GetScheduledMeals([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            int userId = int.Parse(userIdClaim);

            var meals = await _context.MealSchedules
                .Where(m => m.UserId == userId && m.ScheduledDate.Date >= startDate.Date && m.ScheduledDate.Date <= endDate.Date)
                .Include(m => m.Meal)
                .ToListAsync();

            return Ok(meals);
        }

        [HttpPost("Schedule")]
        public async Task<IActionResult> ScheduleMeal([FromBody] MealSchedule mealSchedule)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            mealSchedule.UserId = int.Parse(userIdClaim);

            _context.MealSchedules.Add(mealSchedule);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Meal scheduled successfully!" });
        }


        [HttpPut("UpdateStatus/{id}")]
        public async Task<IActionResult> UpdateMealStatus(int id, [FromBody] string status)
        {
            var mealSchedule = await _context.MealSchedules.FindAsync(id);
            if (mealSchedule == null) return NotFound();

            mealSchedule.Status = status;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = $"Meal marked as {status}!" });
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var mealSchedule = await _context.MealSchedules.FindAsync(id);
            if (mealSchedule == null) return NotFound();

            return View(mealSchedule);
        }

        [HttpPost("Edit/{id}")]
        public async Task<IActionResult> Edit(int id, MealSchedule mealSchedule)
        {
            if (id != mealSchedule.Id) return BadRequest();

            _context.MealSchedules.Update(mealSchedule);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var mealSchedule = await _context.MealSchedules.FindAsync(id);
            if (mealSchedule == null) return NotFound();

            _context.MealSchedules.Remove(mealSchedule);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
          
        [HttpPost("SendReminder")]
        public async Task<IActionResult> SendMealReminder()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            int userId = int.Parse(userIdClaim);

            var user = await _context.User.FindAsync(userId);
            if (user == null) return NotFound("User not found");

            var upcomingMeal = await _context.MealSchedules
                .Where(m => m.UserId == userId && m.ScheduledDate > DateTime.UtcNow && m.ScheduledDate < DateTime.UtcNow.AddHours(1))
                .Include(m => m.Meal)
                .FirstOrDefaultAsync();

            if (upcomingMeal != null)
            {
                await _emailService.SendMealReminder(user.Email, upcomingMeal.Meal.Name, upcomingMeal.Meal.MealType);
                return Ok(new { success = true, message = "Reminder sent!" });
            }

            return Ok(new { success = false, message = "No upcoming meals found." });
        }
    }
}
