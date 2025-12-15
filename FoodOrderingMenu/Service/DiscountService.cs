using FoodOrderingMenu.Data;
using FoodOrderingMenu.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingMenu.Services
{
    public interface IDiscountService
    {
        Task<(bool isValid, string message, decimal discountAmount, DiscountCode? code)> ValidateAndCalculateDiscount(string code, decimal orderTotal);
        Task IncrementUsageCount(int discountCodeId);
    }

    public class DiscountService : IDiscountService
    {
        private readonly AppDbContext _db;

        public DiscountService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<(bool isValid, string message, decimal discountAmount, DiscountCode? code)>
            ValidateAndCalculateDiscount(string code, decimal orderTotal)
        {
            if (string.IsNullOrWhiteSpace(code))
                return (false, "Please enter a discount code", 0, null);

            // Find discount code
            var discountCode = await _db.DiscountCodes
                .FirstOrDefaultAsync(d => d.Code.ToUpper() == code.ToUpper());

            if (discountCode == null)
                return (false, "Invalid discount code", 0, null);

            // Check if active
            if (!discountCode.IsActive)
                return (false, "This discount code is no longer active", 0, null);

            // Check expiry date
            if (discountCode.ExpiryDate.HasValue && discountCode.ExpiryDate.Value < DateTime.UtcNow)
                return (false, "This discount code has expired", 0, null);

            // Check max uses
            if (discountCode.MaxUses.HasValue && discountCode.TimesUsed >= discountCode.MaxUses.Value)
                return (false, "This discount code has reached its usage limit", 0, null);

            // Check minimum order amount
            if (discountCode.MinOrderAmount.HasValue && orderTotal < discountCode.MinOrderAmount.Value)
                return (false, $"Minimum order amount is RM {discountCode.MinOrderAmount.Value:F2}", 0, null);

            // Calculate discount
            decimal discountAmount = Math.Round(orderTotal * (discountCode.DiscountPercentage / 100), 2);

            // Apply max discount cap if set
            if (discountCode.MaxDiscountAmount.HasValue && discountAmount > discountCode.MaxDiscountAmount.Value)
            {
                discountAmount = discountCode.MaxDiscountAmount.Value;
            }

            string message = $"Discount applied! You saved RM {discountAmount:F2} ({discountCode.DiscountPercentage}% off)";

            return (true, message, discountAmount, discountCode);
        }

        public async Task IncrementUsageCount(int discountCodeId)
        {
            var code = await _db.DiscountCodes.FindAsync(discountCodeId);
            if (code != null)
            {
                code.TimesUsed++;
                await _db.SaveChangesAsync();
            }
        }
    }
}