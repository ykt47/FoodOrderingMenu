using FoodOrderingMenu.Data;
using FoodOrderingMenu.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingMenu.Controllers
{
    public class OrdersController : Controller
    {
        private readonly AppDbContext _db;

        public OrdersController(AppDbContext db) => _db = db;

        // GET: Orders/Success/5
        public async Task<IActionResult> Success(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Index", "Menu");
            }

            // Optional: Verify that the order belongs to the current user (if authenticated)
            if (User.Identity?.IsAuthenticated ?? false)
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
                {
                    if (order.UserId != userId)
                    {
                        TempData["Error"] = "You don't have permission to view this order.";
                        return RedirectToAction("Index", "Menu");
                    }
                }
            }

            return View(order);
        }

        // GET: Orders/Track/5
        public async Task<IActionResult> Track(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Index", "Menu");
            }

            // Optional: Verify that the order belongs to the current user (if authenticated)
            if (User.Identity?.IsAuthenticated ?? false)
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
                {
                    if (order.UserId != userId)
                    {
                        TempData["Error"] = "You don't have permission to track this order.";
                        return RedirectToAction("Index", "Menu");
                    }
                }
            }

            return View(order);
        }

        // GET: Orders/TrackPublic/5 (Alternative without authentication check)
        // Use this if you want to allow anyone with the order ID to track it
        public async Task<IActionResult> TrackPublic(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Index", "Menu");
            }

            return View("Track", order);
        }

        // API endpoint for real-time order status updates
        [HttpGet]
        public async Task<IActionResult> GetOrderStatus(int id)
        {
            var order = await _db.Orders
                .Where(o => o.Id == id)
                .Select(o => new
                {
                    id = o.Id,
                    status = o.Status,
                    updatedAt = o.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return Json(new { success = false, message = "Order not found." });
            }

            return Json(new { success = true, data = order });
        }
    }
}