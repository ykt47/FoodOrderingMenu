using FoodOrderingMenu.Data;
using FoodOrderingMenu.Helpers;
using FoodOrderingMenu.Models;
using FoodOrderingMenu.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FoodOrderingMenu.Controllers
{
    public class CheckoutController : Controller
    {
        private const string CART_KEY = "CART";
        private const string DISCOUNT_KEY = "DISCOUNT";
        private readonly AppDbContext _db;
        private readonly IPaymentService _paymentService;
        private readonly IDiscountService _discountService;

        public CheckoutController(
            AppDbContext db,
            IPaymentService paymentService,
            IDiscountService discountService)
        {
            _db = db;
            _paymentService = paymentService;
            _discountService = discountService;
        }

        private List<CartItem> GetCart()
            => HttpContext.Session.GetObject<List<CartItem>>(CART_KEY) ?? new List<CartItem>();

        private void ClearCart()
        {
            HttpContext.Session.Remove(CART_KEY);
            HttpContext.Session.Remove(DISCOUNT_KEY);
        }

        [HttpGet]
        public IActionResult Index()
        {
            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction("Index", "Cart");

            var vm = BuildTotals(cart);

            // Restore discount from session if exists
            var discountData = HttpContext.Session.GetObject<(string code, decimal amount)>(DISCOUNT_KEY);
            if (discountData != default)
            {
                vm.DiscountCodeInput = discountData.code;
                vm.DiscountAmount = discountData.amount;
                vm.IsDiscountApplied = true;
                vm.DiscountMessage = $"Discount applied! You're saving RM {discountData.amount:F2}";
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyDiscount(string discountCode)
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                return Json(new { success = false, message = "Your cart is empty" });
            }

            var totals = BuildTotals(cart);
            var (isValid, message, discountAmount, code) =
                await _discountService.ValidateAndCalculateDiscount(discountCode, totals.GrandTotal);

            if (isValid)
            {
                // Store discount in session
                HttpContext.Session.SetObject(DISCOUNT_KEY, (discountCode, discountAmount));

                return Json(new
                {
                    success = true,
                    message = message,
                    discountAmount = discountAmount,
                    finalTotal = totals.GrandTotal - discountAmount
                });
            }

            return Json(new { success = false, message = message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveDiscount()
        {
            HttpContext.Session.Remove(DISCOUNT_KEY);
            return Json(new { success = true });
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

            // Validate payment method
            if (string.IsNullOrWhiteSpace(vm.PaymentMethod))
            {
                TempData["Error"] = "Please select a payment method.";
                return RedirectToAction("Index");
            }

            // Validate card payment
            if (vm.PaymentMethod == "Card")
            {
                if (!_paymentService.ValidateCardNumber(vm.CardNumber ?? ""))
                {
                    TempData["Error"] = "Invalid card number. Please check and try again.";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrWhiteSpace(vm.CardHolderName))
                {
                    TempData["Error"] = "Card holder name is required.";
                    return RedirectToAction("Index");
                }

                if (!_paymentService.ValidateExpiryDate(vm.ExpiryMonth ?? "", vm.ExpiryYear ?? ""))
                {
                    TempData["Error"] = "Card has expired or invalid expiry date.";
                    return RedirectToAction("Index");
                }

                if (!_paymentService.ValidateCVV(vm.CVV ?? ""))
                {
                    TempData["Error"] = "Invalid CVV code.";
                    return RedirectToAction("Index");
                }
            }

            // Validate E-Wallet
            if (vm.PaymentMethod == "EWallet")
            {
                if (string.IsNullOrWhiteSpace(vm.EWalletProvider))
                {
                    TempData["Error"] = "Please choose an E-Wallet provider.";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrWhiteSpace(vm.EWalletPhone))
                {
                    TempData["Error"] = "Phone number is required for E-Wallet payment.";
                    return RedirectToAction("Index");
                }
            }

            var totals = BuildTotals(cart);

            // Get discount from session
            var discountData = HttpContext.Session.GetObject<(string code, decimal amount)>(DISCOUNT_KEY);
            DiscountCode? discountCode = null;
            decimal discountAmount = 0;

            if (discountData != default)
            {
                var (isValid, _, amount, code) =
                    await _discountService.ValidateAndCalculateDiscount(discountData.code, totals.GrandTotal);

                if (isValid)
                {
                    discountCode = code;
                    discountAmount = amount;
                }
            }

            int? userId = null;
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out var parsed)) userId = parsed;

            // Calculate final total
            decimal finalTotal = totals.GrandTotal - discountAmount;

            // Create order
            var order = new Models.Order
            {
                UserId = userId,
                Status = "Received",
                PaymentMethod = vm.PaymentMethod,
                PaymentProvider = vm.PaymentMethod == "EWallet" ? vm.EWalletProvider :
                                  vm.PaymentMethod == "Card" ? _paymentService.GetCardType(vm.CardNumber ?? "") :
                                  "Cash",
                Subtotal = totals.Subtotal,
                ServiceTax = totals.ServiceTax,
                SST = totals.SST,
                GrandTotal = totals.GrandTotal,
                DiscountCodeId = discountCode?.Id,
                DiscountAmount = discountAmount,
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

            // Process payment with final total
            vm.GrandTotal = finalTotal;
            var paymentResult = await _paymentService.ProcessPayment(vm, order.Id);

            if (paymentResult.Status == "Failed")
            {
                TempData["Error"] = $"Payment failed: {paymentResult.ErrorMessage}";

                _db.Orders.Remove(order);
                await _db.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            // Increment discount usage count
            if (discountCode != null)
            {
                await _discountService.IncrementUsageCount(discountCode.Id);
            }

            // Clear cart and discount
            ClearCart();

            TempData["TransactionId"] = paymentResult.TransactionId;
            TempData["PaymentStatus"] = paymentResult.Status;
            TempData["DiscountSaved"] = discountAmount > 0 ? discountAmount : (decimal?)null;

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