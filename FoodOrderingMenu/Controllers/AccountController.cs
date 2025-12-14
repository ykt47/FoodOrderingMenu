using Microsoft.AspNetCore.Mvc;
using FoodOrderingMenu.Data;
using FoodOrderingMenu.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace FoodOrderingMenu.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        public AccountController(AppDbContext db) => _db = db;

        // -----------------------------
        // REGISTER
        // -----------------------------
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string fullName, string email, string password, string? confirmPassword)
        {
            fullName = fullName?.Trim() ?? "";
            email = (email ?? "").Trim().ToLowerInvariant();
            password = password ?? "";
            confirmPassword = confirmPassword ?? "";

            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Full name, email and password are required.";
                return View();
            }

            // If your Register page has confirm password, enforce it
            if (!string.IsNullOrWhiteSpace(confirmPassword) && password != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View();
            }

            if (await _db.Users.AnyAsync(u => u.Email == email))
            {
                ViewBag.Error = "Email already registered.";
                return View();
            }

            var user = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = Utilities.HashPassword(password),
                EmailConfirmed = true,
                Role = "Customer",
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Registration successful. Please sign in.";
            return RedirectToAction("Login");
        }

        // -----------------------------
        // LOGIN
        // -----------------------------
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            email = (email ?? "").Trim().ToLowerInvariant();
            password = password ?? "";

            // Validation
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Provide email and password.";
                return View();
            }

            // Lockout
            int failedAttempts = HttpContext.Session.GetInt32("FailedAttempts") ?? 0;

            DateTime? lockoutEnd = null;
            var lockoutRaw = HttpContext.Session.GetString("LockoutEnd");
            if (!string.IsNullOrWhiteSpace(lockoutRaw) && DateTime.TryParse(lockoutRaw, out var parsed))
                lockoutEnd = parsed;

            if (lockoutEnd.HasValue && lockoutEnd.Value > DateTime.Now)
            {
                double secondsLeft = Math.Ceiling((lockoutEnd.Value - DateTime.Now).TotalSeconds);
                ViewBag.Error = $"Too many failed attempts. Try again in {secondsLeft} seconds.";
                return View();
            }

            // User lookup
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            bool isValid = user != null && user.PasswordHash == Utilities.HashPassword(password);

            if (!isValid)
            {
                failedAttempts++;
                HttpContext.Session.SetInt32("FailedAttempts", failedAttempts);

                if (failedAttempts >= 5)
                {
                    var end = DateTime.Now.AddSeconds(60);
                    HttpContext.Session.SetString("LockoutEnd", end.ToString("O"));
                    ViewBag.Error = "Too many failed attempts. Account locked for 60 seconds.";
                }
                else
                {
                    int left = 5 - failedAttempts;
                    ViewBag.Error = $"Invalid credentials. Attempts remaining: {left}";
                }

                return View();
            }

            // Success login (reset lockout)
            HttpContext.Session.Remove("FailedAttempts");
            HttpContext.Session.Remove("LockoutEnd");
            HttpContext.Session.Remove("IsGuest");

            var displayName = string.IsNullOrWhiteSpace(user!.FullName) ? user.Email : user.FullName;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, displayName),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, string.IsNullOrWhiteSpace(user.Role) ? "Customer" : user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            TempData["LoginSuccess"] = "Welcome back!";

            if (user.Role == "Admin")
                return RedirectToAction("Index", "Admin");

            return RedirectToAction("Index", "Home");
        }

        // -----------------------------
        // CONTINUE AS GUEST (NO LOGIN FORM)
        // -----------------------------
        [HttpGet]
        public IActionResult Guest()
        {
            // No cookie auth needed; session is enough for cart browsing
            HttpContext.Session.SetString("IsGuest", "1");
            return RedirectToAction("Index", "Home");
        }

        // -----------------------------
        // FORGOT PASSWORD
        // -----------------------------
        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            model.Email = (model.Email ?? "").Trim().ToLowerInvariant();
            model.FullName = (model.FullName ?? "").Trim();

            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.FullName))
            {
                ViewBag.Error = "Please fill in both fields.";
                return View(model);
            }

            var user = await _db.Users.FirstOrDefaultAsync(u =>
                u.Email == model.Email && u.FullName == model.FullName);

            if (user == null)
            {
                ViewBag.Error = "No user found matching the provided name and email.";
                return View(model);
            }

            return RedirectToAction("ResetPassword", new { email = user.Email, fullName = user.FullName });
        }

        // -----------------------------
        // RESET PASSWORD
        // -----------------------------
        [HttpGet]
        public IActionResult ResetPassword(string email, string fullName)
        {
            return View(new ResetPasswordViewModel
            {
                Email = (email ?? "").Trim().ToLowerInvariant(),
                FullName = (fullName ?? "").Trim()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            model.Email = (model.Email ?? "").Trim().ToLowerInvariant();
            model.FullName = (model.FullName ?? "").Trim();

            if (string.IsNullOrWhiteSpace(model.NewPassword))
            {
                ViewBag.Error = "New password is required.";
                return View(model);
            }

            var user = await _db.Users.FirstOrDefaultAsync(u =>
                u.Email == model.Email && u.FullName == model.FullName);

            if (user == null)
            {
                ViewBag.Error = "User not found.";
                return View(model);
            }

            user.PasswordHash = Utilities.HashPassword(model.NewPassword);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Password reset successfully! Please log in.";
            return RedirectToAction("Login", "Account");
        }

        // -----------------------------
        // LOGOUT
        // -----------------------------
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Remove("IsGuest");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}
