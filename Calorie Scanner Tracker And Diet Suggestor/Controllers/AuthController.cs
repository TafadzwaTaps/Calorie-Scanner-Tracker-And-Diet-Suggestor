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

        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(User user)
        {
            if (string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.PasswordHash))
                return BadRequest("Email and Password are required.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            _context.User.Add(user);
            await _context.SaveChangesAsync();
            return RedirectToAction("Login");
        }

        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(User user)
        {
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
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            // Optional: Session
            HttpContext.Session.SetString("Username", dbUser.Username ?? string.Empty);
            HttpContext.Session.SetString("Role", dbUser.Role ?? string.Empty);

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
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (result?.Principal == null)
                return RedirectToAction("Login");

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login");

            var user = await _context.User.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    Username = name ?? "Google User",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                    Role = "User"
                };
                _context.User.Add(user);
                await _context.SaveChangesAsync();
            }

            // Create claims and sign in
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            // Session optional
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role);

            return RedirectToAction("HomePage", "Home");
        }

        public async Task<IActionResult> FacebookResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (result?.Principal == null)
                return RedirectToAction("Login");

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login");

            var user = await _context.User.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    Username = name ?? "Facebook User",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                    Role = "User"
                };
                _context.User.Add(user);
                await _context.SaveChangesAsync();
            }

            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role);

            return RedirectToAction("HomePage", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public IActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = _context.User.SingleOrDefault(u => u.Email == model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "User with the specified email does not exist.");
                return View(model);
            }

            return RedirectToAction("ResetPassword", "Auth", new { email = model.Email });
        }

        [HttpGet]
        public IActionResult ResetPassword(string email) => View(new ResetPasswordViewModel { Email = email });

        [HttpPost]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = _context.User.SingleOrDefault(u => u.Email == model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "User with the specified email does not exist.");
                return View(model);
            }

            // ✅ Hash the new password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
            _context.SaveChanges();

            return RedirectToAction("ResetPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation() => View();
        [HttpGet] public IActionResult AccessDenied() => View();

        public IActionResult Questionnaire(int userId) => View(new UserPreferences { UserId = userId });

        [HttpPost]
        public async Task<IActionResult> Questionnaire(UserPreferences preferences)
        {
            if (!ModelState.IsValid) return View(preferences);

            _context.UserPreferences.Add(preferences);
            await _context.SaveChangesAsync();
            return RedirectToAction("Login");
        }
    }
}