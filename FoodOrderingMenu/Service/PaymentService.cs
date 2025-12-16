using FoodOrderingMenu.Data;
using FoodOrderingMenu.Models;

namespace FoodOrderingMenu.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _db;

        public PaymentService(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Process payment for an order
        /// </summary>
        public async Task<PaymentResult> ProcessPayment(CheckoutViewModel model, int orderId)
        {
            var result = new PaymentResult
            {
                TransactionId = GenerateTransactionId(),
                ProcessedAt = DateTime.UtcNow
            };

            var transaction = new PaymentTransaction
            {
                OrderId = orderId,
                PaymentMethod = model.PaymentMethod,
                Amount = model.FinalTotal, // Use FinalTotal which includes discount
                TransactionId = result.TransactionId,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                switch (model.PaymentMethod)
                {
                    case "PayAtCounter":
                        // Cash payment - always succeeds
                        transaction.Status = "Success";
                        transaction.PaymentProvider = "Cash";
                        transaction.CompletedAt = DateTime.UtcNow;
                        result.Status = "Success";
                        break;

                    case "Card":
                        // Card payment validation
                        if (!ValidateCardNumber(model.CardNumber ?? ""))
                        {
                            transaction.Status = "Failed";
                            transaction.ErrorMessage = "Invalid card number";
                            result.Status = "Failed";
                            result.ErrorMessage = "Invalid card number. Please check and try again.";
                            break;
                        }

                        if (!ValidateCVV(model.CVV ?? ""))
                        {
                            transaction.Status = "Failed";
                            transaction.ErrorMessage = "Invalid CVV";
                            result.Status = "Failed";
                            result.ErrorMessage = "Invalid CVV code.";
                            break;
                        }

                        if (!ValidateExpiryDate(model.ExpiryMonth ?? "", model.ExpiryYear ?? ""))
                        {
                            transaction.Status = "Failed";
                            transaction.ErrorMessage = "Card expired";
                            result.Status = "Failed";
                            result.ErrorMessage = "Card has expired or invalid expiry date.";
                            break;
                        }

                        // Simulate card processing (In real app: call Stripe/PayPal API)
                        await Task.Delay(1000); // Simulate API call

                        // Save card info (last 4 digits only for security)
                        var cardNumber = model.CardNumber?.Replace(" ", "").Replace("-", "") ?? "";
                        transaction.CardLastFourDigits = cardNumber.Length >= 4
                            ? cardNumber.Substring(cardNumber.Length - 4)
                            : cardNumber;
                        transaction.CardType = GetCardType(model.CardNumber ?? "");
                        transaction.PaymentProvider = transaction.CardType;
                        transaction.Status = "Success";
                        transaction.CompletedAt = DateTime.UtcNow;
                        result.Status = "Success";
                        break;

                    case "EWallet":
                        // E-Wallet payment
                        if (string.IsNullOrWhiteSpace(model.EWalletProvider))
                        {
                            transaction.Status = "Failed";
                            transaction.ErrorMessage = "E-Wallet provider not specified";
                            result.Status = "Failed";
                            result.ErrorMessage = "Please select an E-Wallet provider.";
                            break;
                        }

                        // Simulate e-wallet processing
                        await Task.Delay(800);

                        transaction.PaymentProvider = model.EWalletProvider;
                        transaction.Status = "Success";
                        transaction.CompletedAt = DateTime.UtcNow;
                        result.Status = "Success";
                        break;

                    default:
                        transaction.Status = "Failed";
                        transaction.ErrorMessage = "Unknown payment method";
                        result.Status = "Failed";
                        result.ErrorMessage = "Invalid payment method selected.";
                        break;
                }
            }
            catch (Exception ex)
            {
                transaction.Status = "Failed";
                transaction.ErrorMessage = ex.Message;
                result.Status = "Failed";
                result.ErrorMessage = $"Payment processing error: {ex.Message}";
            }

            // Save transaction to database
            _db.PaymentTransactions.Add(transaction);
            await _db.SaveChangesAsync();

            return result;
        }

        /// <summary>
        /// Determine card type from card number
        /// </summary>
        public string GetCardType(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber))
                return "Unknown";

            cardNumber = cardNumber.Replace(" ", "").Replace("-", "");

            if (cardNumber.StartsWith("4"))
                return "Visa";

            if (cardNumber.StartsWith("5"))
                return "Mastercard";

            if (cardNumber.StartsWith("3"))
                return "American Express";

            if (cardNumber.StartsWith("6"))
                return "Discover";

            return "Unknown";
        }

        /// <summary>
        /// Validate card number using Luhn algorithm
        /// </summary>
        public bool ValidateCardNumber(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber))
                return false;

            // Remove spaces and dashes
            cardNumber = cardNumber.Replace(" ", "").Replace("-", "");

            // Check if all characters are digits
            if (!cardNumber.All(char.IsDigit))
                return false;

            // Check length (13-19 digits for most cards)
            if (cardNumber.Length < 13 || cardNumber.Length > 19)
                return false;

            // Luhn Algorithm
            int sum = 0;
            bool alternate = false;

            // Process digits from right to left
            for (int i = cardNumber.Length - 1; i >= 0; i--)
            {
                int digit = cardNumber[i] - '0';

                if (alternate)
                {
                    digit *= 2;
                    if (digit > 9)
                        digit -= 9;
                }

                sum += digit;
                alternate = !alternate;
            }

            // Valid if sum is divisible by 10
            return (sum % 10) == 0;
        }

        /// <summary>
        /// Validate card expiry date
        /// </summary>
        public bool ValidateExpiryDate(string month, string year)
        {
            if (string.IsNullOrWhiteSpace(month) || string.IsNullOrWhiteSpace(year))
                return false;

            if (!int.TryParse(month, out int expiryMonth) || !int.TryParse(year, out int expiryYear))
                return false;

            // Check month is valid (1-12)
            if (expiryMonth < 1 || expiryMonth > 12)
                return false;

            // Handle 2-digit year (e.g., 25 becomes 2025)
            if (expiryYear < 100)
                expiryYear += 2000;

            // Get last day of expiry month
            var expiryDate = new DateTime(expiryYear, expiryMonth, 1)
                .AddMonths(1)
                .AddDays(-1);

            // Card is valid if expiry date is today or in the future
            return expiryDate >= DateTime.Today;
        }

        /// <summary>
        /// Validate CVV code (3 or 4 digits)
        /// </summary>
        public bool ValidateCVV(string cvv)
        {
            if (string.IsNullOrWhiteSpace(cvv))
                return false;

            // CVV should be 3 digits (Visa, MC) or 4 digits (Amex)
            return (cvv.Length == 3 || cvv.Length == 4) && cvv.All(char.IsDigit);
        }

        /// <summary>
        /// Generate unique transaction ID
        /// </summary>
        private string GenerateTransactionId()
        {
            // Format: TXN + YYYYMMDDHHMMSS + Random4Digits
            // Example: TXN202512151430451234
            return $"TXN{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        }
    }
}