using System.Diagnostics;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Database;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly CalorieTrackerContext _context;

        public HomeController(ILogger<HomeController> logger, CalorieTrackerContext context)
        {
            _logger = logger;
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        [HttpGet("")]
        public IActionResult Index() => View();

        [HttpGet("Privacy")]
        public IActionResult Privacy() => View();

        [Authorize]
        [HttpGet("HomePage")]
        public IActionResult HomePage(string token)
        {
            ViewBag.Token = token;
            return View();
        }

        [HttpGet("AboutUs")]
        public IActionResult AboutUs() => View();

        [HttpGet("Contact")]
        public IActionResult Contact() => View();

        [HttpGet("Terms")]
        public IActionResult Terms() => View();

        [Authorize]
        [HttpGet("Account")]
        public async Task<IActionResult> Account()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out int id)) return RedirectToAction("Login", "Auth");

            var user = await _context.User.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound("User not found.");

            return View(user);
        }

        [Authorize]
        [HttpGet("Inbox")]
        public async Task<IActionResult> Inbox()
        {
            var username = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Auth");

            var messages = await _context.Messages
                .Where(m => m.ReceiverId == username)
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();

            return View(messages);
        }

        [Authorize]
        [HttpGet("Settings")]
        public async Task<IActionResult> Settings()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var settings = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);

            if (settings == null)
            {
                settings = new UserSettings { UserId = userId };
                _context.UserSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return View(settings);
        }

        [Authorize]
        [HttpPost("UpdateSettings")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(UserSettings updatedSettings)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var settings = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);
            if (settings == null) return NotFound();

            settings.DailyCalorieTarget = updatedSettings.DailyCalorieTarget;
            settings.MealSchedule = updatedSettings.MealSchedule;
            settings.MealReminders = updatedSettings.MealReminders;
            settings.Language = updatedSettings.Language;
            settings.WaterGlassCapacity = updatedSettings.WaterGlassCapacity;
            settings.MenuDisplaySettings = updatedSettings.MenuDisplaySettings;
            settings.NavigationAppearance = updatedSettings.NavigationAppearance;
            settings.Theme = updatedSettings.Theme;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Settings updated successfully.";
            return RedirectToAction("Settings");
        }

        [Authorize]
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete(".AspNetCore.Cookies");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Login", "Auth");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}