using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FoodOrderingMenu.Data;
using FoodOrderingMenu.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingMenu.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MenuCategoryController : Controller
    {
        private readonly AppDbContext _db;

        public MenuCategoryController(AppDbContext db)
        {
            _db = db;
        }

        /* ----------------------------------------------------------
         *  GET: Index + Pagination
         * ---------------------------------------------------------- */
        public async Task<IActionResult> Index(int page = 1, int pageSize = 8)
        {
            var query = _db.MenuCategories.OrderBy(c => c.SortOrder);

            int totalItems = await query.CountAsync();

            var categories = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.TotalItems = totalItems;

            return View(categories);
        }

        /* ----------------------------------------------------------
         *  GET: Create
         * ---------------------------------------------------------- */
        public IActionResult Create() => View();

        /* ----------------------------------------------------------
         *  POST: Create
         * ---------------------------------------------------------- */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MenuCategory model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _db.MenuCategories.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Category created successfully.";
            return RedirectToAction(nameof(Index));
        }

        /* ----------------------------------------------------------
         *  GET: Edit
         * ---------------------------------------------------------- */
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _db.MenuCategories.FindAsync(id);
            if (category == null) return NotFound();

            return View(category);
        }

        /* ----------------------------------------------------------
         *  POST: Edit
         * ---------------------------------------------------------- */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MenuCategory model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existing = await _db.MenuCategories.FindAsync(model.Id);
            if (existing == null) return NotFound();

            existing.Name = model.Name;
            existing.SortOrder = model.SortOrder;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Category updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        /* ----------------------------------------------------------
         *  POST: Delete
         * ---------------------------------------------------------- */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _db.MenuCategories.FindAsync(id);
            if (category == null) return NotFound();

            _db.MenuCategories.Remove(category);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Category deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
