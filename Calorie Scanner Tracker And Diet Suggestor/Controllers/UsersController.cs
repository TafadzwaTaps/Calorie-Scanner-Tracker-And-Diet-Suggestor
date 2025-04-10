﻿using Calorie_Scanner_Tracker_And_Diet_Suggestor.Database;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Models;
using Google.Apis.Drive.v3.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[Route("Users")]
[Authorize]
public class UsersController : Controller
{
    private readonly CalorieTrackerContext _context;

    public UsersController(CalorieTrackerContext context)
    {
        _context = context;
    }

    [HttpGet] // Explicitly defining HTTP method
    [Route("")]
    public async Task<IActionResult> Index()
    {
        return View(await _context.User.ToListAsync());
    }

    [HttpGet("{id}")] // Explicitly defining the ID in the route
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var user = await _context.User.FirstOrDefaultAsync(m => m.Id == id);
        if (user == null) return NotFound();

        return View(user);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([Bind("Username,Email,PasswordHash,Role")] Calorie_Scanner_Tracker_And_Diet_Suggestor.Models.User users)
    {
        if (string.IsNullOrWhiteSpace(users.Email) || string.IsNullOrWhiteSpace(users.PasswordHash))
        {
            return BadRequest("Email and Password are required.");
        }

        // ✅ Hash the password before storing
        users.PasswordHash = BCrypt.Net.BCrypt.HashPassword(users.PasswordHash);

        if (ModelState.IsValid)
        {
            _context.Add(users);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(users);
    }

    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var user = await _context.User.FindAsync(id);
        if (user == null) return NotFound();

        return View(user);
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Username,Email,PasswordHash,Role")] Calorie_Scanner_Tracker_And_Diet_Suggestor.Models.User users)
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
                if (!_context.User.Any(e => e.Id == users.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(users);
    }

    [HttpGet("Delete/{id}")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var user = await _context.User.FirstOrDefaultAsync(m => m.Id == id);
        if (user == null) return NotFound();

        return View(user);
    }

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await _context.User.FindAsync(id);
        if (user != null)
        {
            _context.User.Remove(user);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Profile")]
    public async Task<IActionResult> Profile()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var user = await _context.User.FindAsync(userId);

        if (user == null) return NotFound();

        return View(user);
    }

    [HttpGet("AdminDashboard")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminDashboard()
    {
        var users = await _context.User.ToListAsync();
        return View(users);
    }

    [HttpPost("SetRole/{id}/{role}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetRole(int id, string role)
    {
        var user = await _context.User.FindAsync(id);
        if (user == null) return NotFound();

        if (role != "Admin" && role != "User") return BadRequest("Invalid role.");

        user.Role = role;
        await _context.SaveChangesAsync();
        return RedirectToAction("AdminDashboard");
    }
}
