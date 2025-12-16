using FoodOrderingMenu.Data;
using FoodOrderingMenu.Models;
using FoodOrderingMenu.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace FoodOrderingMenu.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        private readonly ICaptchaService _captchaService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public AccountController(AppDbContext db, ICaptchaService captchaService, IEmailService emailService, IConfiguration configuration)
        {
            _db = db;
            _captchaService = captchaService;
            _emailService = emailService;
            _configuration = configuration;
        }
        // Add this to your AccountController temporarily for testing

        [HttpGet]
        public IActionResult CaptchaTest()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CaptchaTest(string test)
        {
            var gRecaptchaResponse = Request.Form["g-recaptcha-response"].ToString();

            ViewBag.ReceivedResponse = gRecaptchaResponse;
            ViewBag.ResponseLength = gRecaptchaResponse?.Length ?? 0;

            if (string.IsNullOrEmpty(gRecaptchaResponse))
            {
                ViewBag.Result = "ERROR: No CAPTCHA response received";
                return View();
            }

            // Test the CAPTCHA service
            var isValid = await _captchaService.VerifyCaptcha(gRecaptchaResponse);

            ViewBag.Result = isValid ? "SUCCESS!" : "FAILED";

            // Also test directly with HttpClient
            var secretKey = _configuration["ReCaptcha:SecretKey"];
            using var client = new HttpClient();
            var url = $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={gRecaptchaResponse}";
            var response = await client.PostAsync(url, null);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            ViewBag.GoogleResponse = jsonResponse;
            ViewBag.SecretKey = secretKey?.Substring(0, 10) + "..."; // Show first 10 chars only

            return View();
        }
        // GET: Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: Register
        [HttpPost]
        public async Task<IActionResult> Register(User user, string password, string confirmPassword)
        {
            // Get CAPTCHA from form data - THIS IS THE FIX!
            var gRecaptchaResponse = Request.Form["g-recaptcha-response"].ToString();

            // Verify CAPTCHA
            if (string.IsNullOrEmpty(gRecaptchaResponse))
            {
                TempData["Error"] = "Please complete the CAPTCHA verification";
                return View(user);
            }

            bool isCaptchaValid = await _captchaService.VerifyCaptcha(gRecaptchaResponse);
            if (!isCaptchaValid)
            {
                TempData["Error"] = "CAPTCHA verification failed. Please try again.";
                return View(user);
            }

            // Validate passwords match
            if (password != confirmPassword)
            {
                TempData["Error"] = "Passwords do not match";
                return View(user);
            }

            // Check if email already exists
            if (await _db.Users.AnyAsync(u => u.Email == user.Email))
            {
                TempData["Error"] = "Email already registered";
                return View(user);
            }

            // Create user
            user.PasswordHash = Utilities.HashPassword(password);
            user.EmailConfirmed = false; // Not confirmed yet
            user.Role = "Customer";
            user.CreatedAt = DateTime.UtcNow;

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Generate verification token
            var token = GenerateVerificationToken(user.Id);
            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? Request.Scheme + "://" + Request.Host;
            var verificationLink = $"{baseUrl}/Account/VerifyEmail?userId={user.Id}&token={token}";

            // Send verification email
            bool emailSent = await _emailService.SendVerificationEmail(user.Email, user.FullName, verificationLink);

            if (!emailSent)
            {
                TempData["Error"] = "Account created but failed to send verification email. Please contact support.";
                return RedirectToAction("Login");
            }

            // Show email sent page
            return View("EmailSent");
        }

        // GET: Verify Email
        public async Task<IActionResult> VerifyEmail(int userId, string token)
        {
            var user = await _db.Users.FindAsync(userId);

            if (user == null)
            {
                TempData["Error"] = "Invalid verification link";
                return RedirectToAction("Login");
            }

            if (user.EmailConfirmed)
            {
                TempData["Success"] = "Email already verified. Please login.";
                return RedirectToAction("Login");
            }

            // Verify token
            var expectedToken = GenerateVerificationToken(userId);
            if (token != expectedToken)
            {
                TempData["Error"] = "Invalid or expired verification link";
                return RedirectToAction("Login");
            }

            // Mark email as confirmed
            user.EmailConfirmed = true;
            await _db.SaveChangesAsync();

            // Show success page
            return View("EmailVerified");
        }

        // GET: Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Login
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            // Get CAPTCHA from form data - THIS IS THE FIX!
            var gRecaptchaResponse = Request.Form["g-recaptcha-response"].ToString();

            // Verify CAPTCHA
            if (string.IsNullOrEmpty(gRecaptchaResponse))
            {
                TempData["Error"] = "Please complete the CAPTCHA verification";
                return View();
            }

            bool isCaptchaValid = await _captchaService.VerifyCaptcha(gRecaptchaResponse);
            if (!isCaptchaValid)
            {
                TempData["Error"] = "CAPTCHA verification failed. Please try again.";
                return View();
            }

            // Find user
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                TempData["Error"] = "Invalid email or password";
                return View();
            }

            // Check email is verified
            if (!user.EmailConfirmed)
            {
                TempData["Error"] = "Please verify your email address before logging in. Check your inbox for the verification link.";
                return View();
            }

            // Verify password
            var hashedPassword = Utilities.HashPassword(password);
            if (user.PasswordHash != hashedPassword)
            {
                TempData["Error"] = "Invalid email or password";
                return View();
            }

            // Create claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Redirect based on role
            if (user.Role == "Admin")
                return RedirectToAction("Index", "Admin");
            else
                return RedirectToAction("Index", "Home");
        }

        // GET: Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // GET: Forgot Password
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Forgot Password
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string fullName, string email)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email && u.FullName == fullName);

            if (user == null)
            {
                TempData["Error"] = "No account found with this name and email";
                return View();
            }

            // Generate reset token
            var token = GenerateVerificationToken(user.Id);
            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? Request.Scheme + "://" + Request.Host;
            var resetLink = $"{baseUrl}/Account/ResetPassword?userId={user.Id}&token={token}";

            // Send reset email
            await _emailService.SendPasswordResetEmail(user.Email, user.FullName, resetLink);

            TempData["Success"] = "Password reset link has been sent to your email";
            return RedirectToAction("Login");
        }

        // GET: Reset Password
        public async Task<IActionResult> ResetPassword(int userId, string token)
        {
            var user = await _db.Users.FindAsync(userId);

            if (user == null)
            {
                TempData["Error"] = "Invalid reset link";
                return RedirectToAction("Login");
            }

            // Verify token
            var expectedToken = GenerateVerificationToken(userId);
            if (token != expectedToken)
            {
                TempData["Error"] = "Invalid or expired reset link";
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel
            {
                Email = user.Email,
                FullName = user.FullName
            };

            ViewBag.UserId = userId;
            ViewBag.Token = token;

            return View(model);
        }

        // POST: Reset Password
        [HttpPost]
        public async Task<IActionResult> ResetPassword(int userId, string token, string newPassword)
        {
            var user = await _db.Users.FindAsync(userId);

            if (user == null)
            {
                TempData["Error"] = "Invalid request";
                return RedirectToAction("Login");
            }

            // Verify token
            var expectedToken = GenerateVerificationToken(userId);
            if (token != expectedToken)
            {
                TempData["Error"] = "Invalid or expired reset link";
                return RedirectToAction("Login");
            }

            // Update password
            user.PasswordHash = Utilities.HashPassword(newPassword);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Password reset successfully. Please login with your new password.";
            return RedirectToAction("Login");
        }

        // Guest Checkout
        public IActionResult GuestCheckout()
        {
            HttpContext.Session.SetString("IsGuest", "true");
            return RedirectToAction("Index", "Menu");
        }

        // Helper: Generate verification token
        private string GenerateVerificationToken(int userId)
        {
            var data = $"{userId}-{DateTime.UtcNow:yyyyMMdd}";
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }
    }
}