using Microsoft.AspNetCore.Mvc;
using FoodOrderingMenu.Data;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingMenu.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        public HomeController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            ViewBag.Categories = await _db.MenuCategories.OrderBy(c => c.SortOrder).ToListAsync();
            var items = await _db.MenuItems.Include(m => m.Category).Where(m => m.IsAvailable).OrderBy(m => m.Name).ToListAsync();
            return View(items);
        }
    }
}
