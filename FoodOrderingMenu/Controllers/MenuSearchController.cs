using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodOrderingMenu.Data;   // update namespace to match your project
using System.Linq;
using System.Threading.Tasks;

namespace FoodOrderingMenu.Controllers
{
    public class MenuSearchController : Controller
    {
        private readonly AppDbContext _db;
        public MenuSearchController(AppDbContext db) => _db = db;

        // show the search UI
        public async Task<IActionResult> Index()
        {
            ViewBag.Categories = await _db.MenuCategories.OrderBy(c => c.SortOrder).ToListAsync();
            return View();
        }

        // AJAX endpoint - returns JSON (not HTML)
        [HttpGet]
        public async Task<IActionResult> Search(string keyword, int? categoryId, decimal? minPrice, decimal? maxPrice)
        {
            var q = _db.MenuItems.Include(m => m.Category).AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var t = keyword.Trim();
                q = q.Where(m => EF.Functions.Like(m.Name, $"%{t}%") ||
                                 EF.Functions.Like(m.Description, $"%{t}%"));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
                q = q.Where(m => m.CategoryId == categoryId.Value);

            if (minPrice.HasValue)
                q = q.Where(m => m.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                q = q.Where(m => m.Price <= maxPrice.Value);

            var list = await q.OrderBy(m => m.Name)
                              .Select(m => new
                              {
                                  id = m.Id,
                                  name = m.Name,
                                  description = m.Description,
                                  price = m.Price,
                                  imageUrl = m.ImageUrl ?? "",
                                  category = m.Category != null ? m.Category.Name : "",
                                  isAvailable = m.IsAvailable
                              })
                              .ToListAsync();

            return Json(list);
        }
    }
}
