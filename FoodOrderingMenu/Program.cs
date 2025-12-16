using FoodOrderingMenu.Data;
using FoodOrderingMenu.Services; // ADD THIS
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Authentication cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.Cookie.Name = "FoodMenuAuth";
    });

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
});

// ✨ ADD THESE SERVICE REGISTRATIONS ✨
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IDiscountService, DiscountService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHttpClient<ICaptchaService, CaptchaService>();

var app = builder.Build();

// Error handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// OPTIONAL: Seed ONLY ADMIN (NO MENU RESET)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Seed ONLY the admin user if missing
    if (!db.Users.Any(u => u.Role == "Admin"))
    {
        db.Users.Add(new FoodOrderingMenu.Models.User
        {
            FullName = "Admin User",
            Email = "admin@local",
            PasswordHash = Utilities.HashPassword("Admin123"),
            EmailConfirmed = true,
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        });

        db.SaveChanges();
    }
}

app.Run();