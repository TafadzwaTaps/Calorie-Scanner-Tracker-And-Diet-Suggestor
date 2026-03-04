using Calorie_Scanner_Tracker_And_Diet_Suggestor.Database;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Controllers
{
    [Authorize]
    [Route("FoodLogs")]
    public class FoodLogsController : Controller
    {
        private readonly CalorieTrackerContext _context;

        public FoodLogsController(CalorieTrackerContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        #region INDEX - View Logs
        [HttpGet("")]
        public async Task<IActionResult> Index(DateTime? date)
        {
            int userId = GetUserId();
            DateTime selectedDate = date ?? DateTime.Today;

            var logs = await _context.FoodLogs
                .Include(f => f.Meal)
                .Where(f => f.UserId == userId &&
                            f.DateLogged.Date == selectedDate.Date)
                .OrderByDescending(f => f.DateLogged)
                .ToListAsync();

            ViewBag.SelectedDate = selectedDate;

            ViewBag.TotalCalories = logs.Sum(l => l.Calories);
            ViewBag.TotalProtein = logs.Sum(l => l.Protein);
            ViewBag.TotalCarbs = logs.Sum(l => l.Carbs);
            ViewBag.TotalFats = logs.Sum(l => l.Fats);

            return View(logs);
        }
        #endregion

        #region DASHBOARD SUMMARY
        [HttpGet("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            int userId = GetUserId();

            var logs = await _context.FoodLogs
                .Where(f => f.UserId == userId)
                .ToListAsync();

            var grouped = logs
                .GroupBy(l => l.DateLogged.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Calories = g.Sum(x => x.Calories),
                    Protein = g.Sum(x => x.Protein),
                    Carbs = g.Sum(x => x.Carbs),
                    Fats = g.Sum(x => x.Fats)
                })
                .OrderBy(x => x.Date)
                .ToList();

            return View(grouped);
        }
        #endregion

        #region JSON DATA FOR CHARTS
        [HttpGet("ChartData")]
        public async Task<IActionResult> ChartData()
        {
            int userId = GetUserId();

            var logs = await _context.FoodLogs
                .Where(f => f.UserId == userId)
                .ToListAsync();

            var grouped = logs
                .GroupBy(l => l.DateLogged.Date)
                .Select(g => new
                {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    calories = g.Sum(x => x.Calories)
                })
                .OrderBy(x => x.date)
                .ToList();

            return Json(grouped);
        }
        #endregion

        #region DELETE LOG
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            int userId = GetUserId();

            var log = await _context.FoodLogs
                .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);

            if (log == null)
                return NotFound();

            _context.FoodLogs.Remove(log);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Food log deleted.";
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region TOGGLE EATEN
        [HttpPost("ToggleEaten")]
        public async Task<IActionResult> ToggleEaten(int id)
        {
            int userId = GetUserId();

            var log = await _context.FoodLogs
                .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);

            if (log == null)
                return NotFound();

            log.IsEaten = !log.IsEaten;
            log.DateLogged = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, eaten = log.IsEaten });
        }
        #endregion
    }
}