using FoodOrderingMenu.Data;
using FoodOrderingMenu.Helpers;
using FoodOrderingMenu.Models;
using FoodOrderingMenu.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FoodOrderingMenu.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IPaymentService _paymentService;
        private readonly IDiscountService _discountService;

        public CheckoutController(AppDbContext db, IPaymentService paymentService, IDiscountService discountService)
        {
            _db = db;
            _paymentService = paymentService;
            _discountService = discountService;
        }

        public IActionResult Index()
        {
            // Get cart items from session
            var cart = HttpContext.Session.GetObject<List<CartItem>>("CART") ?? new List<CartItem>();

            if (!cart.Any())
            {
                TempData["Error"] = "Your cart is empty!";
                return RedirectToAction("Index", "Cart");
            }

            // Calculate totals
            decimal subtotal = cart.Sum(c => c.LineTotal);
            decimal serviceTax = subtotal * 0.10m; // 10%
            decimal sst = subtotal * 0.06m;        // 6%
            decimal grandTotal = subtotal + serviceTax + sst;

            // Get discount from session (if any)
            bool isDiscountApplied = false;
            decimal discountAmount = 0;
            string discountMessage = "";
            int? discountCodeId = null;

            try
            {
                var discountJson = HttpContext.Session.GetString("DISCOUNT");

                if (!string.IsNullOrEmpty(discountJson))
                {
                    var discountData = JsonSerializer.Deserialize<DiscountSessionData>(discountJson);

                    if (discountData != null)
                    {
                        isDiscountApplied = true;
                        discountAmount = discountData.DiscountAmount;
                        discountMessage = discountData.Message;
                        discountCodeId = discountData.DiscountCodeId;
                    }
                }
            }
            catch
            {
                // If there's any error reading discount, just ignore it
                isDiscountApplied = false;
            }

            var model = new CheckoutViewModel
            {
                Items = cart,
                Subtotal = subtotal,
                ServiceTax = serviceTax,
                SST = sst,
                GrandTotal = grandTotal, // Grand Total WITHOUT discount
                DiscountAmount = discountAmount,
                IsDiscountApplied = isDiscountApplied,
                DiscountMessage = discountMessage,
                PaymentMethod = "PayAtCounter" // Default
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ApplyDiscount(string discountCode)
        {
            // Get cart
            var cart = HttpContext.Session.GetObject<List<CartItem>>("CART") ?? new List<CartItem>();

            if (!cart.Any())
            {
                TempData["Error"] = "Your cart is empty!";
                return RedirectToAction("Index", "Cart");
            }

            // Calculate subtotal
            decimal subtotal = cart.Sum(c => c.LineTotal);
            decimal serviceTax = subtotal * 0.10m;
            decimal sst = subtotal * 0.06m;
            decimal grandTotal = subtotal + serviceTax + sst;

            // Validate discount code
            var (isValid, message, discountAmount, code) = await _discountService.ValidateAndCalculateDiscount(discountCode, grandTotal);

            if (isValid && code != null)
            {
                // Store discount in session
                var discountData = new DiscountSessionData
                {
                    DiscountCodeId = code.Id,
                    DiscountCode = code.Code,
                    DiscountAmount = discountAmount,
                    Message = message
                };

                var json = JsonSerializer.Serialize(discountData);
                HttpContext.Session.SetString("DISCOUNT", json);

                TempData["Success"] = message;
            }
            else
            {
                TempData["Error"] = message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult RemoveDiscount()
        {
            // Remove discount from session
            HttpContext.Session.Remove("DISCOUNT");
            TempData["Success"] = "Discount code removed.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Pay(CheckoutViewModel model)
        {
            // Get cart
            var cart = HttpContext.Session.GetObject<List<CartItem>>("CART") ?? new List<CartItem>();

            if (!cart.Any())
            {
                TempData["Error"] = "Your cart is empty!";
                return RedirectToAction("Index", "Cart");
            }

            // Calculate totals
            decimal subtotal = cart.Sum(c => c.LineTotal);
            decimal serviceTax = subtotal * 0.10m;
            decimal sst = subtotal * 0.06m;
            decimal grandTotal = subtotal + serviceTax + sst;

            // Get discount from session
            int? discountCodeId = null;
            decimal discountAmount = 0;
            decimal finalTotal = grandTotal; // Start with grand total

            try
            {
                var discountJson = HttpContext.Session.GetString("DISCOUNT");

                if (!string.IsNullOrEmpty(discountJson))
                {
                    var discountData = JsonSerializer.Deserialize<DiscountSessionData>(discountJson);

                    if (discountData != null)
                    {
                        discountCodeId = discountData.DiscountCodeId;
                        discountAmount = discountData.DiscountAmount;

                        // Calculate final total with discount
                        finalTotal = grandTotal - discountAmount;
                    }
                }
            }
            catch
            {
                // If error, continue without discount
            }

            // Get user ID (if logged in)
            int? userId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int parsedUserId))
                {
                    userId = parsedUserId;
                }
            }

            // Validate payment method specific fields
            if (model.PaymentMethod == "Card")
            {
                if (!_paymentService.ValidateCardNumber(model.CardNumber ?? ""))
                {
                    TempData["Error"] = "Invalid card number. Please check and try again.";
                    return RedirectToAction("Index");
                }

                if (!_paymentService.ValidateCVV(model.CVV ?? ""))
                {
                    TempData["Error"] = "Invalid CVV code.";
                    return RedirectToAction("Index");
                }

                if (!_paymentService.ValidateExpiryDate(model.ExpiryMonth ?? "", model.ExpiryYear ?? ""))
                {
                    TempData["Error"] = "Card has expired or invalid expiry date.";
                    return RedirectToAction("Index");
                }
            }
            else if (model.PaymentMethod == "EWallet")
            {
                if (string.IsNullOrWhiteSpace(model.EWalletProvider))
                {
                    TempData["Error"] = "Please select an E-Wallet provider.";
                    return RedirectToAction("Index");
                }
            }

            // Create order
            var order = new Order
            {
                UserId = userId,
                Status = "Received",
                PaymentMethod = model.PaymentMethod,
                PaymentProvider = model.PaymentMethod == "EWallet" ? model.EWalletProvider : null,
                Subtotal = subtotal,
                ServiceTax = serviceTax,
                SST = sst,
                GrandTotal = finalTotal, // Store the final amount after discount
                DiscountCodeId = discountCodeId,
                DiscountAmount = discountAmount,
                CreatedAt = DateTime.UtcNow,
                Items = cart.Select(c => new OrderItem
                {
                    MenuItemId = c.MenuItemId,
                    Name = c.Name,
                    UnitPrice = c.UnitPrice,
                    Qty = c.Qty,
                    Sweetness = c.Sweetness,
                    IceLevel = c.IceLevel
                }).ToList()
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            // Update checkout model with totals for payment processing
            model.Subtotal = subtotal;
            model.ServiceTax = serviceTax;
            model.SST = sst;
            model.GrandTotal = finalTotal; // Pass the final total after discount
            model.DiscountAmount = discountAmount;

            // Process payment
            var paymentResult = await _paymentService.ProcessPayment(model, order.Id);

            if (paymentResult.Status != "Success")
            {
                // Payment failed - update order status
                order.Status = "Cancelled";
                order.CancellationReason = paymentResult.ErrorMessage ?? "Payment failed";
                await _db.SaveChangesAsync();

                TempData["Error"] = $"Payment failed: {paymentResult.ErrorMessage}";
                return RedirectToAction("Index");
            }

            // Increment discount code usage
            if (discountCodeId.HasValue)
            {
                await _discountService.IncrementUsageCount(discountCodeId.Value);
            }

            // Clear cart and discount from session
            HttpContext.Session.Remove("CART");
            HttpContext.Session.Remove("DISCOUNT");

            TempData["Success"] = "Order placed successfully!";
            return RedirectToAction("Success", "Orders", new { id = order.Id });
        }
    }

    // Helper class for storing discount in session
    public class DiscountSessionData
    {
        public int DiscountCodeId { get; set; }
        public string DiscountCode { get; set; } = "";
        public decimal DiscountAmount { get; set; }
        public string Message { get; set; } = "";
    }
}