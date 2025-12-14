using FoodOrderingMenu.Data;
using FoodOrderingMenu.Helpers;
using FoodOrderingMenu.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingMenu.Controllers
{
    public class CartController : Controller
    {
        private const string CART_KEY = "CART";
        private readonly AppDbContext _db;

        public CartController(AppDbContext db) => _db = db;

        private List<CartItem> GetCart()
            => HttpContext.Session.GetObject<List<CartItem>>(CART_KEY) ?? new List<CartItem>();

        private void SaveCart(List<CartItem> cart)
            => HttpContext.Session.SetObject(CART_KEY, cart);

        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        // /Cart/Add?id=1&qty=1&sweet=50&ice=Normal%20ice
        public async Task<IActionResult> Add(int id, int qty = 1, string? sweet = null, string? ice = null)
        {
            if (qty < 1) qty = 1;

            var item = await _db.MenuItems.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id && m.IsAvailable);
            if (item == null) return NotFound();

            var cart = GetCart();

            // treat different drink options as different cart lines
            var existing = cart.FirstOrDefault(x =>
                x.MenuItemId == id &&
                string.Equals(x.Sweetness ?? "", sweet ?? "", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.IceLevel ?? "", ice ?? "", StringComparison.OrdinalIgnoreCase));

            if (existing != null) existing.Qty += qty;
            else
            {
                cart.Add(new CartItem
                {
                    MenuItemId = item.Id,
                    Name = item.Name,
                    ImageUrl = item.ImageUrl,
                    UnitPrice = item.Price,
                    Qty = qty,
                    Sweetness = sweet,
                    IceLevel = ice
                });
            }

            SaveCart(cart);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateQty(int id, int qty, string? sweet = null, string? ice = null)
        {
            var cart = GetCart();
            var line = cart.FirstOrDefault(x =>
                x.MenuItemId == id &&
                string.Equals(x.Sweetness ?? "", sweet ?? "", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.IceLevel ?? "", ice ?? "", StringComparison.OrdinalIgnoreCase));

            if (line != null)
            {
                if (qty <= 0) cart.Remove(line);
                else line.Qty = qty;
                SaveCart(cart);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Remove(int id, string? sweet = null, string? ice = null)
        {
            var cart = GetCart();
            cart.RemoveAll(x =>
                x.MenuItemId == id &&
                string.Equals(x.Sweetness ?? "", sweet ?? "", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.IceLevel ?? "", ice ?? "", StringComparison.OrdinalIgnoreCase));

            SaveCart(cart);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(CART_KEY);
            return RedirectToAction("Index");
        }
    }
}
