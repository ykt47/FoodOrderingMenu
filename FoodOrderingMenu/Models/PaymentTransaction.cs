using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderingMenu.Models
{
    public class PaymentTransaction
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order? Order { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = ""; // Cash, Card, EWallet

        [StringLength(50)]
        public string? PaymentProvider { get; set; } // Visa, Mastercard, TNG, etc.

        [StringLength(100)]
        public string TransactionId { get; set; } = ""; // Generated transaction ID

        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Success, Failed, Cancelled

        // Card details (encrypted/masked in real app)
        [StringLength(4)]
        public string? CardLastFourDigits { get; set; } // Only store last 4 digits

        [StringLength(20)]
        public string? CardType { get; set; } // Visa, Mastercard, Amex

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        [StringLength(500)]
        public string? ErrorMessage { get; set; }
    }
}