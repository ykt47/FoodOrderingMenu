using FoodOrderingMenu.Data;
using FoodOrderingMenu.Helpers;
using FoodOrderingMenu.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FoodOrderingMenu.Controllers
{
    public class CheckoutController : Controller
    {
        private const string CART_KEY = "CART";
        private readonly AppDbContext _db;

        public CheckoutController(AppDbContext db) => _db = db;

        private List<CartItem> GetCart()
            => HttpContext.Session.GetObject<List<CartItem>>(CART_KEY) ?? new List<CartItem>();

        private void ClearCart() => HttpContext.Session.Remove(CART_KEY);

        [HttpGet]
        public IActionResult Index()
        {
            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction("Index", "Cart");

            var vm = BuildTotals(cart);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(CheckoutViewModel vm)
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index", "Cart");
            }

            // validate payment selection
            if (string.IsNullOrWhiteSpace(vm.PaymentMethod))
            {
                TempData["Error"] = "Please select a payment method.";
                return RedirectToAction("Index");
            }

            if (vm.PaymentMethod == "EWallet" && string.IsNullOrWhiteSpace(vm.EWalletProvider))
            {
                TempData["Error"] = "Please choose an E-Wallet provider.";
                return RedirectToAction("Index");
            }

            var totals = BuildTotals(cart);

            int? userId = null;
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out var parsed)) userId = parsed;

            var order = new Models.Order
            {
                UserId = userId,
                Status = "Received",
                PaymentMethod = vm.PaymentMethod,
                PaymentProvider = vm.PaymentMethod == "EWallet" ? vm.EWalletProvider : null,
                Subtotal = totals.Subtotal,
                ServiceTax = totals.ServiceTax,
                SST = totals.SST,
                GrandTotal = totals.GrandTotal,
                CreatedAt = DateTime.UtcNow,
            };

            foreach (var c in cart)
            {
                order.Items.Add(new OrderItem
                {
                    MenuItemId = c.MenuItemId,
                    Name = c.Name,
                    UnitPrice = c.UnitPrice,
                    Qty = c.Qty,
                    Sweetness = c.Sweetness,
                    IceLevel = c.IceLevel
                });
            }

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            ClearCart();
            return RedirectToAction("Success", "Orders", new { id = order.Id });
        }

        private CheckoutViewModel BuildTotals(List<CartItem> cart)
        {
            var subtotal = cart.Sum(x => x.LineTotal);
            var serviceTax = Math.Round(subtotal * 0.10m, 2);
            var sst = Math.Round(subtotal * 0.06m, 2);
            var grand = subtotal + serviceTax + sst;

            return new CheckoutViewModel
            {
                Items = cart,
                Subtotal = Math.Round(subtotal, 2),
                ServiceTax = serviceTax,
                SST = sst,
                GrandTotal = Math.Round(grand, 2)
            };
        }
    }
}
