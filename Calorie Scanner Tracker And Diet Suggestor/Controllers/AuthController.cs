using Calorie_Scanner_Tracker_And_Diet_Suggestor.Database;
using Calorie_Scanner_Tracker_And_Diet_Suggestor.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Controllers
{
    public class AuthController : Controller
    {

        private readonly CalorieTrackerContext _context;
        public AuthController(CalorieTrackerContext context)
        {
            _context = context;
        }
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(User user)
        {
            if (string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return BadRequest("Email and Password are required.");
            }

            // ✅ Hash the password before storing
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);

            _context.User.Add(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(User user)
        {
            Console.WriteLine($"[DEBUG] Email: {user.Email}");
            Console.WriteLine($"[DEBUG] Password: {user.Password}"); // Check if null

            if (string.IsNullOrEmpty(user.Password))
            {
                ModelState.AddModelError("", "Password is required.");
                return View();
            }

            var dbUser = await _context.User.FirstOrDefaultAsync(u => u.Email == user.Email);

            if (dbUser == null || !BCrypt.Net.BCrypt.Verify(user.Password, dbUser.PasswordHash))
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View();
            }

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, dbUser.Id.ToString()),
        new Claim(ClaimTypes.Name, dbUser.Username),
        new Claim(ClaimTypes.Email, dbUser.Email),
        new Claim(ClaimTypes.Role, dbUser.Role)
    };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            HttpContext.Session.SetString("Username", dbUser.Username ?? string.Empty);

            return RedirectToAction("HomePage", "Home");
        }



        public IActionResult LoginWithGoogle()
        {
            var redirectUrl = Url.Action("GoogleResponse", "Auth");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, "Google");
        }

        public IActionResult LoginWithFacebook()
        {
            var redirectUrl = Url.Action("FacebookResponse", "Auth");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, "Facebook");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (result?.Principal == null)
            {
                return RedirectToAction("Login");
            }

            // Extract user details
            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (email == null)
            {
                return RedirectToAction("Login");
            }

            // Check if user exists in the database
            var user = await _context.User.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                // Register new user
                user = new User
                {
                    Email = email,
                    Username = name ?? "Google User",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Google users don't have passwords
                    Role = "User"
                };

                _context.User.Add(user);
                await _context.SaveChangesAsync();
            }

            // Create authentication claims
            var userClaims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Role, user.Role)
    };

            var identity = new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Home");
        }


        public async Task<IActionResult> FacebookResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (result?.Principal == null)
            {
                return RedirectToAction("Login");
            }

            var claims = result.Principal.Identities
                .FirstOrDefault()?.Claims.Select(c => new { c.Type, c.Value });

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        public IActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.User.SingleOrDefault(u => u.Email == model.Email);
                if (user != null)
                {
                    // Redirect to ResetPassword page with user's email as a parameter
                    return RedirectToAction("ResetPassword", "Account", new { email = model.Email });
                }
                ModelState.AddModelError("", "User with the specified email does not exist.");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            var model = new ResetPasswordViewModel { Email = email };
            return View(model);
        }

        [HttpPost]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.User.SingleOrDefault(u => u.Email == model.Email);
                if (user != null)
                {
                    user.PasswordHash = (model.Password);
                    _context.SaveChanges();
                    return RedirectToAction("ResetPasswordConfirmation");
                }
                ModelState.AddModelError("", "User with the specified email does not exist.");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }


        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        public IActionResult Questionnaire(int userId)
        {
            var userPreferences = new UserPreferences { UserId = userId };
            return View(userPreferences);
        }

        [HttpPost]
        public async Task<IActionResult> Questionnaire(UserPreferences preferences)
        {
            if (ModelState.IsValid)
            {
                _context.UserPreferences.Add(preferences);
                await _context.SaveChangesAsync();

                // Redirect to login or dashboard after completion
                return RedirectToAction("Login");
            }
            return View(preferences);
        }
    }
}