using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderingMenu.Models
{
    public class Order
    {
        public int Id { get; set; }

        public int? UserId { get; set; }
        public User? User { get; set; }

        [Required] public string Status { get; set; } = "Received"; // Received/Preparing/Ready
        [Required] public string PaymentMethod { get; set; } = "PayAtCounter"; // PayAtCounter/Card/EWallet
        public string? PaymentProvider { get; set; } // for EWallet options

        [Column(TypeName = "decimal(10,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal ServiceTax { get; set; } // 10%

        [Column(TypeName = "decimal(10,2)")]
        public decimal SST { get; set; } // 6%

        [Column(TypeName = "decimal(10,2)")]
        public decimal GrandTotal { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? CancellationReason { get; set; }
        public List<OrderItem> Items { get; set; } = new();

        public int? DiscountCodeId { get; set; }
        public DiscountCode? DiscountCode { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal DiscountAmount { get; set; } = 0;
    }
}
