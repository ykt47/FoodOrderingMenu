using FoodOrderingMenu.Models;

namespace FoodOrderingMenu.Services
{
    /// <summary>
    /// Payment result data transfer object
    /// </summary>
    public class PaymentResult
    {
        public string TransactionId { get; set; } = "";
        public string Status { get; set; } = "Pending"; // Success, Failed, Pending
        public string? ErrorMessage { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Payment service interface for processing payments
    /// </summary>
    public interface IPaymentService
    {
        /// <summary>
        /// Process payment and return transaction result
        /// </summary>
        Task<PaymentResult> ProcessPayment(CheckoutViewModel model, int orderId);

        /// <summary>
        /// Get card type from card number (Visa, Mastercard, etc.)
        /// </summary>
        string GetCardType(string cardNumber);

        /// <summary>
        /// Validate card number using Luhn algorithm
        /// </summary>
        bool ValidateCardNumber(string cardNumber);

        /// <summary>
        /// Validate expiry date (check if card is not expired)
        /// </summary>
        bool ValidateExpiryDate(string month, string year);

        /// <summary>
        /// Validate CVV code (3-4 digits)
        /// </summary>
        bool ValidateCVV(string cvv);
    }
}