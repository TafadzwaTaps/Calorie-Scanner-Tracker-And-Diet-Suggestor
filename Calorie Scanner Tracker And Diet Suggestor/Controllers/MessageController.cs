using Calorie_Scanner_Tracker_And_Diet_Suggestor.Database;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Controllers
{
    [Authorize]
    [Route("Messages")]
    public class MessageController : Controller
    {
        private readonly CalorieTrackerContext _context;

        public MessageController(CalorieTrackerContext context)
        {
            _context = context;
        }

        private string GetUsername()
        {
            return User.FindFirstValue(ClaimTypes.Name);
        }

        #region INBOX
        [HttpGet("Inbox")]
        public async Task<IActionResult> Inbox(string? searchTerm, int page = 1, int pageSize = 10)
        {
            string username = GetUsername();

            var query = _context.Messages
                .Where(m => m.ReceiverId == username);

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(m =>
                    m.Content.Contains(searchTerm) ||
                    m.SenderId.Contains(searchTerm));

            int totalCount = await query.CountAsync();

            var messages = await query
                .OrderByDescending(m => m.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return View(messages);
        }
        #endregion

        #region SENT MESSAGES
        [HttpGet("Sent")]
        public async Task<IActionResult> Sent(string? searchTerm, int page = 1, int pageSize = 10)
        {
            string username = GetUsername();

            var query = _context.Messages
                .Where(m => m.SenderId == username);

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(m =>
                    m.Content.Contains(searchTerm) ||
                    m.ReceiverId.Contains(searchTerm));

            int totalCount = await query.CountAsync();

            var messages = await query
                .OrderByDescending(m => m.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return View(messages);
        }
        #endregion

        #region MESSAGE DETAILS
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            string username = GetUsername();

            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.MessageId == id &&
                    (m.ReceiverId == username || m.SenderId == username));

            if (message == null)
                return NotFound();

            return View(message);
        }
        #endregion

        #region CREATE MESSAGE
        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Users = await _context.Users
                .Select(u => u.Username)
                .ToListAsync();

            return View();
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Message message)
        {
            string sender = GetUsername();

            if (string.IsNullOrEmpty(message.ReceiverId))
                return BadRequest("Receiver required.");

            bool receiverExists = await _context.Users
                .AnyAsync(u => u.Username == message.ReceiverId);

            if (!receiverExists)
                return BadRequest("Receiver does not exist.");

            message.SenderId = sender;
            message.Timestamp = DateTime.UtcNow;

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Message sent successfully.";
            return RedirectToAction(nameof(Sent));
        }
        #endregion

        #region AJAX SEND
        [HttpPost("SendAjax")]
        public async Task<IActionResult> SendAjax([FromBody] Message message)
        {
            string sender = GetUsername();

            if (string.IsNullOrEmpty(message.ReceiverId) || string.IsNullOrEmpty(message.Content))
                return BadRequest("Invalid message.");

            bool receiverExists = await _context.Users
                .AnyAsync(u => u.Username == message.ReceiverId);

            if (!receiverExists)
                return NotFound("Receiver not found.");

            message.SenderId = sender;
            message.Timestamp = DateTime.UtcNow;

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
        #endregion

        #region DELETE MESSAGE
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            string username = GetUsername();

            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.MessageId == id &&
                    (m.ReceiverId == username || m.SenderId == username));

            if (message == null)
                return NotFound();

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Message deleted.";
            return RedirectToAction(nameof(Inbox));
        }
        #endregion
    }
}