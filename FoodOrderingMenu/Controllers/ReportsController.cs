using FoodOrderingMenu.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FoodOrderingMenu.Controllers
{
    public class ReportsController : Controller
    {
        private readonly AppDbContext _db;

        public ReportsController(AppDbContext db) => _db = db;

        // ========== CUSTOMER ORDER HISTORY ==========
        [Authorize]
        public async Task<IActionResult> MyOrders()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index", "Home");
            }

            var orders = await _db.Orders
                .Include(o => o.Items)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        // ========== ADMIN ORDER HISTORY ==========
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> OrderHistory(DateTime? startDate, DateTime? endDate, string? status)
        {
            var query = _db.Orders
                .Include(o => o.Items)
                .Include(o => o.User)
                .AsQueryable();

            // Date filtering
            if (startDate.HasValue)
            {
                query = query.Where(o => o.CreatedAt.Date >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                query = query.Where(o => o.CreatedAt.Date <= endDate.Value.Date);
            }

            // Status filtering
            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(o => o.Status == status);
            }

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.Status = status ?? "All";

            return View(orders);
        }

        // ========== SALES REPORTS ==========
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SalesReport(string period = "today")
        {
            DateTime startDate, endDate;

            switch (period.ToLower())
            {
                case "today":
                    startDate = DateTime.Today;
                    endDate = DateTime.Today.AddDays(1);
                    break;
                case "week":
                    startDate = DateTime.Today.AddDays(-7);
                    endDate = DateTime.Today.AddDays(1);
                    break;
                case "month":
                    startDate = DateTime.Today.AddMonths(-1);
                    endDate = DateTime.Today.AddDays(1);
                    break;
                case "year":
                    startDate = DateTime.Today.AddYears(-1);
                    endDate = DateTime.Today.AddDays(1);
                    break;
                default:
                    startDate = DateTime.Today;
                    endDate = DateTime.Today.AddDays(1);
                    break;
            }

            var orders = await _db.Orders
                .Include(o => o.Items)
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt < endDate && o.Status != "Cancelled")
                .ToListAsync();

            // Calculate metrics
            var totalRevenue = orders.Sum(o => o.GrandTotal);
            var totalOrders = orders.Count;
            var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;
            var completedOrders = orders.Count(o => o.Status == "Completed");

            // Top selling items
            var topItems = await _db.OrderItems
                .Include(oi => oi.MenuItem)
                .Where(oi => oi.Order.CreatedAt >= startDate && oi.Order.CreatedAt < endDate && oi.Order.Status != "Cancelled")
                .GroupBy(oi => new { oi.MenuItemId, oi.MenuItem.Name })
                .Select(g => new
                {
                    ItemName = g.Key.Name,
                    TotalQuantity = g.Sum(x => x.Qty),
                    TotalRevenue = g.Sum(x => x.UnitPrice * x.Qty)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(10)
                .ToListAsync();

            // Daily sales (for charts)
            var dailySales = orders
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(o => o.GrandTotal),
                    OrderCount = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList();

            // Payment method breakdown
            var paymentBreakdown = orders
                .GroupBy(o => o.PaymentMethod)
                .Select(g => new
                {
                    Method = g.Key,
                    Count = g.Count(),
                    Revenue = g.Sum(o => o.GrandTotal)
                })
                .ToList();

            ViewBag.Period = period;
            ViewBag.StartDate = startDate.ToString("dd MMM yyyy");
            ViewBag.EndDate = endDate.AddDays(-1).ToString("dd MMM yyyy");
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.AvgOrderValue = avgOrderValue;
            ViewBag.CompletedOrders = completedOrders;
            ViewBag.TopItems = topItems;
            ViewBag.DailySales = dailySales;
            ViewBag.PaymentBreakdown = paymentBreakdown;

            return View();
        }

        // ========== AJAX: SEARCH ORDERS ==========
        [HttpGet]
        public async Task<IActionResult> SearchOrders(string keyword, string? status, DateTime? startDate, DateTime? endDate)
        {
            var query = _db.Orders
                .Include(o => o.Items)
                .Include(o => o.User)
                .AsQueryable();

            // Keyword search
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var lowerKeyword = keyword.ToLower();
                query = query.Where(o =>
                    o.Id.ToString().Contains(keyword) ||
                    (o.User != null && (o.User.FullName.ToLower().Contains(lowerKeyword) || o.User.Email.ToLower().Contains(lowerKeyword)))
                );
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(o => o.Status == status);
            }

            // Date range
            if (startDate.HasValue)
            {
                query = query.Where(o => o.CreatedAt.Date >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                query = query.Where(o => o.CreatedAt.Date <= endDate.Value.Date);
            }

            var results = await query
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    id = o.Id,
                    orderDate = o.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy hh:mm tt"),
                    customerName = o.User != null ? o.User.FullName : "Guest",
                    customerEmail = o.User != null ? o.User.Email : "",
                    itemCount = o.Items.Count,
                    total = o.GrandTotal,
                    paymentMethod = o.PaymentMethod,
                    status = o.Status
                })
                .Take(50) // Limit results
                .ToListAsync();

            return Json(results);
        }

        // ========== NOTIFICATION SYSTEM (Future enhancement) ==========
        [Authorize(Roles = "Admin")]
        public IActionResult Notifications()
        {
            // Placeholder for future email notification system
            ViewBag.Message = "Notification system coming soon!";
            return View();
        }
    }
}