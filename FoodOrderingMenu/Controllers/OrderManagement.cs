using FoodOrderingMenu.Data;
using FoodOrderingMenu.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingMenu.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class OrderManagementController : Controller
    {
        private readonly AppDbContext _db;

        public OrderManagementController(AppDbContext db) => _db = db;

        // GET: OrderManagement
        public async Task<IActionResult> Index(string status = "All")
        {
            var query = _db.Orders
                .Include(o => o.Items)
                .Include(o => o.User)
                .AsQueryable();

            // Filter by status
            if (status != "All")
            {
                query = query.Where(o => o.Status == status);
            }

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.ReceivedCount = await _db.Orders.CountAsync(o => o.Status == "Received");
            ViewBag.PendingCount = await _db.Orders.CountAsync(o => o.Status == "Pending");
            ViewBag.PreparingCount = await _db.Orders.CountAsync(o => o.Status == "Preparing");
            ViewBag.ReadyCount = await _db.Orders.CountAsync(o => o.Status == "Ready");
            ViewBag.CompletedCount = await _db.Orders.CountAsync(o => o.Status == "Completed");
            ViewBag.CancelledCount = await _db.Orders.CountAsync(o => o.Status == "Cancelled");

            return View(orders);
        }

        // GET: OrderManagement/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.MenuItem)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Index");
            }

            return View(order);
        }

        // POST: OrderManagement/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus)
        {
            var order = await _db.Orders.FindAsync(id);

            if (order == null)
            {
                return Json(new { success = false, message = "Order not found." });
            }

            // Validate status transition
            var validTransitions = GetValidStatusTransitions(order.Status);
            if (!validTransitions.Contains(newStatus))
            {
                return Json(new { success = false, message = $"Cannot change status from {order.Status} to {newStatus}." });
            }

            order.Status = newStatus;
            order.UpdatedAt = DateTime.UtcNow;

            // Set completion time if order is completed
            if (newStatus == "Completed")
            {
                order.CompletedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = $"Order #{order.Id} status updated to {newStatus}.";
            return Json(new { success = true, message = $"Status updated to {newStatus}" });
        }

        // POST: OrderManagement/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string cancellationReason)
        {
            var order = await _db.Orders.FindAsync(id);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Index");
            }

            // Can only cancel orders that are not completed or already cancelled
            if (order.Status == "Completed" || order.Status == "Cancelled")
            {
                TempData["Error"] = $"Cannot cancel an order that is already {order.Status}.";
                return RedirectToAction("Details", new { id });
            }

            order.Status = "Cancelled";
            order.CancellationReason = cancellationReason;
            order.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["Success"] = $"Order #{order.Id} has been cancelled.";
            return RedirectToAction("Details", new { id });
        }

        // POST: OrderManagement/MoveToNextStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveToNextStatus(int id)
        {
            var order = await _db.Orders.FindAsync(id);

            if (order == null)
            {
                return Json(new { success = false, message = "Order not found." });
            }

            var nextStatus = GetNextStatus(order.Status);
            if (nextStatus == null)
            {
                return Json(new { success = false, message = "No next status available." });
            }

            order.Status = nextStatus;
            order.UpdatedAt = DateTime.UtcNow;

            if (nextStatus == "Completed")
            {
                order.CompletedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            return Json(new { success = true, message = $"Order moved to {nextStatus}", newStatus = nextStatus });
        }

        // Helper method to get valid status transitions
        private List<string> GetValidStatusTransitions(string currentStatus)
        {
            return currentStatus switch
            {
                "Received" => new List<string> { "Pending", "Cancelled" },
                "Pending" => new List<string> { "Preparing", "Cancelled" },
                "Preparing" => new List<string> { "Ready", "Cancelled" },
                "Ready" => new List<string> { "Completed", "Cancelled" },
                "Completed" => new List<string>(),
                "Cancelled" => new List<string>(),
                _ => new List<string>()
            };
        }

        // Helper method to get next status in workflow
        private string GetNextStatus(string currentStatus)
        {
            return currentStatus switch
            {
                "Received" => "Pending",
                "Pending" => "Preparing",
                "Preparing" => "Ready",
                "Ready" => "Completed",
                _ => null
            };
        }

        // API endpoint for real-time order updates
        [HttpGet]
        public async Task<IActionResult> GetOrderCounts()
        {
            var counts = new
            {
                received = await _db.Orders.CountAsync(o => o.Status == "Received"),
                pending = await _db.Orders.CountAsync(o => o.Status == "Pending"),
                preparing = await _db.Orders.CountAsync(o => o.Status == "Preparing"),
                ready = await _db.Orders.CountAsync(o => o.Status == "Ready"),
                completed = await _db.Orders.CountAsync(o => o.Status == "Completed"),
                cancelled = await _db.Orders.CountAsync(o => o.Status == "Cancelled")
            };

            return Json(counts);
        }
    }
}