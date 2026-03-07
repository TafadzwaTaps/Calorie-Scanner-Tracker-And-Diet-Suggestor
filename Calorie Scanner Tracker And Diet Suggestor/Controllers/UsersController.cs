using Calorie_Scanner_Tracker_And_Diet_Suggestor.Database;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Controllers
{
    [Authorize]
    [Route("Users")]
    public class UsersController : Controller
    {
        private readonly CalorieTrackerContext _context;

        public UsersController(CalorieTrackerContext context)
        {
            _context = context;
        }

        // GET: Users
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Users.ToListAsync());
        }

        // GET: Users/Details/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();
            return View(user);
        }

        // GET: Users/Create
        [HttpGet("Create")]
        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        // POST: Users/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Username,Email,PasswordHash,Role")] Users users)
        {
            if (string.IsNullOrWhiteSpace(users.Email) || string.IsNullOrWhiteSpace(users.PasswordHash))
                return BadRequest("Email and Password are required.");

            users.PasswordHash = BCrypt.Net.BCrypt.HashPassword(users.PasswordHash);

            if (ModelState.IsValid)
            {
                _context.Add(users);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(users);
        }

        // GET: Users/Edit/5
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Username,Email,PasswordHash,Role")] Users users)
        {
            if (id != users.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(users);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(users.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            return View(users);
        }

        // GET: Users/Delete/5
        [HttpGet("Delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(m => m.Id == id);
            if (user == null) return NotFound();
            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Users/Profile
        [HttpGet("Profile")]
        public async Task<IActionResult> Profile()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();
            return View(user);
        }

        // GET: Users/AdminDashboard
        [HttpGet("AdminDashboard")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // POST: Users/SetRole/5/Admin
        [HttpPost("SetRole/{id}/{role}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetRole(int id, string role)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (role != "Admin" && role != "User") return BadRequest("Invalid role.");

            user.Role = role;
            await _context.SaveChangesAsync();
            return RedirectToAction("AdminDashboard");
        }

        private bool UserExists(int id) => _context.Users.Any(e => e.Id == id);
    }
}