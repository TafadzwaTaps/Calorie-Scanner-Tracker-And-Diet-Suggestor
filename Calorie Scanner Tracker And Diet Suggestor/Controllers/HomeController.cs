using System.Diagnostics;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Database;
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
            _context = context ?? throw new ArgumentNullException(nameof(context)); // Ensure context is not null
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }


        [Authorize]
        public IActionResult HomePage(string token)
        {
            ViewBag.Token = token;  // Optionally pass it to the view
            return View();
        }


        public IActionResult AboutUs()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }


        public async Task<IActionResult> Account()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Login", "Auth"); // Redirect if user is not logged in
            }

            if (!int.TryParse(userIdString, out int userId))
            {
                return BadRequest("Invalid user ID.");
            }

            var user = await _context.User.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            return View(user);
        }


        [Authorize]
        public async Task<IActionResult> Inbox()
        {
            var username = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (username == null)
            {
                TempData["ErrorMessage"] = "Please log in to access your inbox.";
                return RedirectToAction("Login", "Auth");
            }

            // Fetch messages where the user is the recipient
            var messages = await _context.Messages
                .Where(m => m.ReceiverId == username)
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();

            return View(messages);
        }

        [Authorize]
        public async Task<IActionResult> Settings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Please log in to access settings.";
                return RedirectToAction("Login", "Auth");
            }

            var settings = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == int.Parse(userId));
            if (settings == null)
            {
                settings = new UserSettings { UserId = int.Parse(userId) };
                _context.UserSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return View(settings);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSettings(UserSettings updatedSettings)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var settings = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == int.Parse(userId));
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

        public async Task<IActionResult> Logout()
        {
            // Clear session data
            HttpContext.Session.Clear();

            // Manually remove authentication cookies
            Response.Cookies.Delete(".AspNetCore.Cookies");

            // Sign out the user (Ensure await)
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // ✅ Set flash message before redirecting
            TempData["SuccessMessage"] = "You have been logged out successfully.";

            // Redirect to Login or Home instead of LogOut (ensure correct controller)
            return RedirectToAction("Login", "Auth");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
