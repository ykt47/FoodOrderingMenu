using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FoodOrderingMenu.Data;
using FoodOrderingMenu.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Hosting;

namespace FoodOrderingMenu.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MenuItemController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public MenuItemController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        /* ----------------------------------------------------------
         *  Helper: Load Categories for Dropdown
         * ---------------------------------------------------------- */
        private async Task LoadCategories()
        {
            ViewBag.Categories = new SelectList(
                await _db.MenuCategories.OrderBy(c => c.SortOrder).ToListAsync(),
                "Id",
                "Name"
            );
        }

        /* ----------------------------------------------------------
         *  Helper: Save image file and return URL
         * ---------------------------------------------------------- */
        private async Task<string> SaveImage(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return "";

            string folder = Path.Combine(_env.WebRootPath, "images", "menu_items");
            Directory.CreateDirectory(folder);

            string ext = Path.GetExtension(imageFile.FileName);
            string filename = $"{Guid.NewGuid():N}{ext}";

            string fullPath = Path.Combine(folder, filename);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return $"/images/menu_items/{filename}";
        }

        /* ----------------------------------------------------------
         *  GET: Index + Pagination
         * ---------------------------------------------------------- */
        public async Task<IActionResult> Index(int page = 1, int pageSize = 8)
        {
            var query = _db.MenuItems
                .Include(m => m.Category)
                .OrderBy(m => m.Name);

            int totalItems = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.TotalItems = totalItems;

            return View(items);
        }

        /* ----------------------------------------------------------
         *  GET: Create
         * ---------------------------------------------------------- */
        public async Task<IActionResult> Create()
        {
            await LoadCategories();
            return View();
        }

        /* ----------------------------------------------------------
         *  POST: Create
         * ---------------------------------------------------------- */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MenuItem model)
        {
            await LoadCategories();

            if (!ModelState.IsValid)
                return View(model);

            if (model.ImageFile != null)
                model.ImageUrl = await SaveImage(model.ImageFile);

            _db.MenuItems.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Menu item created successfully.";
            return RedirectToAction(nameof(Index));
        }

        /* ----------------------------------------------------------
         *  GET: Edit
         * ---------------------------------------------------------- */
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _db.MenuItems.FindAsync(id);
            if (item == null) return NotFound();

            await LoadCategories();
            return View(item);
        }

        /* ----------------------------------------------------------
         *  POST: Edit
         * ---------------------------------------------------------- */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MenuItem model)
        {
            await LoadCategories();

            if (!ModelState.IsValid)
                return View(model);

            var existing = await _db.MenuItems.FindAsync(model.Id);
            if (existing == null) return NotFound();

            // New image uploaded?
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                // Delete old file
                if (!string.IsNullOrEmpty(existing.ImageUrl))
                {
                    string oldPath = Path.Combine(_env.WebRootPath, existing.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                existing.ImageUrl = await SaveImage(model.ImageFile);
            }

            // Update fields
            existing.Name = model.Name;
            existing.Description = model.Description;
            existing.Price = model.Price;
            existing.CategoryId = model.CategoryId;
            existing.IsAvailable = model.IsAvailable;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Menu item updated.";
            return RedirectToAction(nameof(Index));
        }

        /* ----------------------------------------------------------
         *  POST: Delete
         * ---------------------------------------------------------- */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.MenuItems.FindAsync(id);
            if (item == null) return NotFound();

            // Delete image from disk
            if (!string.IsNullOrEmpty(item.ImageUrl))
            {
                string fullPath = Path.Combine(_env.WebRootPath, item.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }

            _db.MenuItems.Remove(item);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Menu item deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
