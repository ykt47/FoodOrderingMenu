using FoodOrderingMenu.Models;
using FoodOrderingMenu.Data;

namespace FoodOrderingMenu.Services
{
    public interface IPaymentService
    {
        Task<PaymentTransaction> ProcessPayment(CheckoutViewModel model, int orderId);
        string GetCardType(string cardNumber);
        bool ValidateCardNumber(string cardNumber);
        bool ValidateExpiryDate(string month, string year);
        bool ValidateCVV(string cvv);
    }

    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _db;

        public PaymentService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<PaymentTransaction> ProcessPayment(CheckoutViewModel model, int orderId)
        {
            var transaction = new PaymentTransaction
            {
                OrderId = orderId,
                PaymentMethod = model.PaymentMethod,
                Amount = model.GrandTotal,
                TransactionId = GenerateTransactionId(),
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                switch (model.PaymentMethod)
                {
                    case "PayAtCounter":
                        transaction.Status = "Pending";
                        transaction.PaymentProvider = "Cash";
                        break;

                    case "Card":
                        // Validate card
                        if (!ValidateCardNumber(model.CardNumber ?? ""))
                        {
                            transaction.Status = "Failed";
                            transaction.ErrorMessage = "Invalid card number";
                            break;
                        }

                        // Simulate card processing (In real app: call Stripe/PayPal API)
                        await Task.Delay(1000); // Simulate API call

                        transaction.CardLastFourDigits = model.CardNumber?.Substring(model.CardNumber.Length - 4);
                        transaction.CardType = GetCardType(model.CardNumber ?? "");
                        transaction.Status = "Success";
                        transaction.CompletedAt = DateTime.UtcNow;
                        break;

                    case "EWallet":
                        // Simulate e-wallet processing
                        await Task.Delay(800);

                        transaction.PaymentProvider = model.EWalletProvider;
                        transaction.Status = "Success";
                        transaction.CompletedAt = DateTime.UtcNow;
                        break;

                    default:
                        transaction.Status = "Failed";
                        transaction.ErrorMessage = "Unknown payment method";
                        break;
                }
            }
            catch (Exception ex)
            {
                transaction.Status = "Failed";
                transaction.ErrorMessage = ex.Message;
            }

            _db.PaymentTransactions.Add(transaction);
            await _db.SaveChangesAsync();

            return transaction;
        }

        public string GetCardType(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber)) return "Unknown";

            cardNumber = cardNumber.Replace(" ", "").Replace("-", "");

            if (cardNumber.StartsWith("4")) return "Visa";
            if (cardNumber.StartsWith("5")) return "Mastercard";
            if (cardNumber.StartsWith("3")) return "American Express";
            if (cardNumber.StartsWith("6")) return "Discover";

            return "Unknown";
        }

        public bool ValidateCardNumber(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber)) return false;

            cardNumber = cardNumber.Replace(" ", "").Replace("-", "");

            // Luhn Algorithm (basic card validation)
            if (cardNumber.Length < 13 || cardNumber.Length > 19) return false;

            int sum = 0;
            bool alternate = false;

            for (int i = cardNumber.Length - 1; i >= 0; i--)
            {
                if (!char.IsDigit(cardNumber[i])) return false;

                int digit = cardNumber[i] - '0';

                if (alternate)
                {
                    digit *= 2;
                    if (digit > 9) digit -= 9;
                }

                sum += digit;
                alternate = !alternate;
            }

            return sum % 10 == 0;
        }

        public bool ValidateExpiryDate(string month, string year)
        {
            if (!int.TryParse(month, out int m) || !int.TryParse(year, out int y))
                return false;

            if (m < 1 || m > 12) return false;

            var expiry = new DateTime(y, m, 1).AddMonths(1).AddDays(-1);
            return expiry >= DateTime.Today;
        }

        public bool ValidateCVV(string cvv)
        {
            if (string.IsNullOrWhiteSpace(cvv)) return false;
            return cvv.Length == 3 || cvv.Length == 4;
        }

        private string GenerateTransactionId()
        {
            return $"TXN{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        }
    }
}