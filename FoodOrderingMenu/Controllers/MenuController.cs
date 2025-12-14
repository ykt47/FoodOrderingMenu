using Microsoft.AspNetCore.Mvc;
using FoodOrderingMenu.Data;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingMenu.Controllers
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class MenuController : Controller
    {
        private readonly AppDbContext _db;
        public MenuController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index(int? categoryId, string? search)
        {
            // Load categories
            ViewBag.Categories = await _db.MenuCategories
                .OrderBy(c => c.SortOrder)
                .ToListAsync();

            // Base query for AVAILABLE items only
            var items = _db.MenuItems
                .Include(m => m.Category)
                .Where(m => m.IsAvailable);

            // Filter by category
            if (categoryId.HasValue)
                items = items.Where(i => i.CategoryId == categoryId.Value);

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                items = items.Where(i =>
                    i.Name.ToLower().Contains(search) ||
                    i.Description.ToLower().Contains(search) ||
                    i.Category.Name.ToLower().Contains(search)
                );
            }

            return View(await items.OrderBy(i => i.Name).ToListAsync());
        }
    }
}
