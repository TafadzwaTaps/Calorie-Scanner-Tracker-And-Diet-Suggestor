using Calorie_Scanner_Tracker_And_Diet_Suggestor.Database;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Controllers
{
    [Authorize]
    [Route("MealSchedule")]
    public class MealScheduleController : Controller
    {
        private readonly CalorieTrackerContext _context;

        public MealScheduleController(CalorieTrackerContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        #region DAILY SCHEDULE VIEW
        [HttpGet("")]
        public async Task<IActionResult> Index(DateTime? date)
        {
            int userId = GetUserId();
            DateTime selectedDate = date ?? DateTime.Today;

            var scheduledMeals = await _context.MealSchedules
                .Include(ms => ms.Meal)
                .Where(ms => ms.UserId == userId && ms.ScheduledDate.Date == selectedDate.Date)
                .OrderBy(ms => ms.ScheduledDate)
                .ToListAsync();

            // Get user calorie target
            var settings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.UserId == userId);

            int calorieTarget = settings?.DailyCalorieTarget ?? 2000;

            int consumedCalories = await _context.FoodLogs
                .Where(f => f.UserId == userId && f.DateLogged.Date == selectedDate.Date)
                .SumAsync(f => (int?)f.Calories) ?? 0;

            ViewBag.SelectedDate = selectedDate;
            ViewBag.CalorieTarget = calorieTarget;
            ViewBag.ConsumedCalories = consumedCalories;
            ViewBag.RemainingCalories = calorieTarget - consumedCalories;

            return View(scheduledMeals);
        }
        #endregion

        #region CREATE SCHEDULE
        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Meals = await _context.Meals.ToListAsync();
            return View();
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MealSchedule schedule)
        {
            schedule.UserId = GetUserId();

            if (!ModelState.IsValid)
            {
                ViewBag.Meals = await _context.Meals.ToListAsync();
                return View(schedule);
            }

            _context.MealSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Meal scheduled successfully.";
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region EDIT SCHEDULE
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var schedule = await _context.MealSchedules.FindAsync(id);
            if (schedule == null || schedule.UserId != GetUserId())
                return NotFound();

            ViewBag.Meals = await _context.Meals.ToListAsync();
            return View(schedule);
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MealSchedule schedule)
        {
            if (id != schedule.Id)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                ViewBag.Meals = await _context.Meals.ToListAsync();
                return View(schedule);
            }

            schedule.UserId = GetUserId();

            _context.Update(schedule);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Schedule updated.";
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region DELETE SCHEDULE
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var schedule = await _context.MealSchedules
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == GetUserId());

            if (schedule == null)
                return NotFound();

            _context.MealSchedules.Remove(schedule);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Scheduled meal removed.";
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region CALORIE-BASED SUGGESTIONS
        [HttpGet("Suggestions")]
        public async Task<IActionResult> Suggestions()
        {
            int userId = GetUserId();

            var settings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.UserId == userId);

            int calorieTarget = settings?.DailyCalorieTarget ?? 2000;

            int consumedCalories = await _context.FoodLogs
                .Where(f => f.UserId == userId && f.DateLogged.Date == DateTime.Today)
                .SumAsync(f => (int?)f.Calories) ?? 0;

            int remainingCalories = calorieTarget - consumedCalories;

            var suggestions = await _context.Meals
                .Where(m => m.Calories <= remainingCalories)
                .OrderBy(m => m.Calories)
                .Take(10)
                .ToListAsync();

            ViewBag.RemainingCalories = remainingCalories;

            return View(suggestions);
        }
        #endregion

        #region QUICK ADD TO SCHEDULE (AJAX)
        [HttpPost("QuickAdd")]
        public async Task<IActionResult> QuickAdd(int mealId, DateTime date, TimeSpan time)
        {
            int userId = GetUserId();

            var meal = await _context.Meals.FindAsync(mealId);
            if (meal == null)
                return NotFound();

            var schedule = new MealSchedule
            {
                UserId = userId,
                MealId = mealId,
                ScheduledDate = date,
                ScheduleTime = time
            };

            _context.MealSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
        #endregion
    }
}