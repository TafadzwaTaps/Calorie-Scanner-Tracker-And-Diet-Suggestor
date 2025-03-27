using Calorie_Scanner_Tracker_And_Diet_Suggestor.Database;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Controllers
{
    public class MessageController : Controller
    {

        private readonly CalorieTrackerContext _context;
        private readonly ILogger<MessageController> _logger;

        public MessageController(CalorieTrackerContext context, ILogger<MessageController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [Authorize]
        public async Task<IActionResult> Inbox(string search, int page = 1)
        {
            var username = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (username == null)
            {
                _logger.LogWarning("Unauthenticated user attempted to access the Inbox.");
                return RedirectToAction("Login", "Account");
            }

            // Define the number of items per page
            int pageSize = 10;

            // Get the base query for messages
            IQueryable<Message> query = _context.Messages
        .Where(m => m.ReceiverId == username);

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(m =>
                    m.SenderId.Contains(search) ||
                    m.Content.Contains(search));
            }

            // Apply ordering
            var orderedQuery = query.OrderByDescending(m => m.Timestamp);

            // Pagination
            var totalMessages = await orderedQuery.CountAsync();
            var messages = await orderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Pass data to the view using ViewData
            ViewData["Search"] = search;
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling((double)totalMessages / pageSize);

            return View(messages);
        }




        [Authorize]
        [HttpGet]
        public IActionResult SendMessage()
        {
            var username = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (username == null)
            {
                _logger.LogWarning("Unauthenticated user attempted to access SendMessage view.");
                return RedirectToAction("Login", "Account");
            }

            _logger.LogInformation("User {Username} accessed the SendMessage view.", username);

            return View();
        }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SendMessage(string receiverUsername, string content)
        {
            var username = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (username == null)
            {
                _logger.LogWarning("Unauthenticated user attempted to send a message.");
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(receiverUsername))
            {
                _logger.LogWarning("Message sending failed: Receiver username is null or empty.");
                ModelState.AddModelError("", "Receiver username cannot be empty.");
                return View();
            }

            // Check if the receiver exists
            var receiver = await _context.User.FirstOrDefaultAsync(u => u.Username == receiverUsername);

            if (receiver == null)
            {
                _logger.LogWarning("Message sending failed: Receiver username '{ReceiverUsername}' not found.", receiverUsername);
                ModelState.AddModelError("", "The specified receiver does not exist.");
                return View();
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("Message sending failed: Content is empty.");
                ModelState.AddModelError("", "Message content cannot be empty.");
                return View();
            }

            var message = new Message
            {
                SenderId = username,
                ReceiverId = receiver.Username, // Use the receiver's username
                Content = content,
                Timestamp = DateTime.Now,
                IsRead = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {SenderUsername} sent a message to {ReceiverUsername}.", username, receiverUsername);

            return RedirectToAction("Inbox");
        }
    }
}
