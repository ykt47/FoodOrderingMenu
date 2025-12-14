using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FoodOrderingMenu.Data;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingMenu.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;

        public AdminController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            // Total users (registered customers + admins)
            ViewBag.TotalUsers = await _db.Users.CountAsync();

            // Total Orders
            ViewBag.TotalOrders = await _db.Orders.CountAsync();

            ViewBag.TotalCategories = await _db.MenuCategories.CountAsync();


            // Best selling items (Top 5)
            var bestSelling = await _db.OrderItems
                .GroupBy(oi => oi.MenuItemId)
                .Select(g => new
                {
                    ItemId = g.Key,
                    TotalQty = g.Sum(x => x.Qty),
                    Name = g.First().MenuItem.Name
                })
                .OrderByDescending(x => x.TotalQty)
                .Take(5)
                .ToListAsync();

            ViewBag.BestSellingLabels = bestSelling.Select(b => b.Name).ToList();
            ViewBag.BestSellingValues = bestSelling.Select(b => b.TotalQty).ToList();

            return View();
        }
    }
}
