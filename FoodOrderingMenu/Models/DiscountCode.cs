using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderingMenu.Models
{
    public class DiscountCode
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Discount Code")]
        public string Code { get; set; } = ""; // e.g., "SAVE10", "NEWUSER"

        [Required]
        [StringLength(100)]
        public string Description { get; set; } = "";

        [Required]
        [Range(1, 100)]
        [Display(Name = "Discount %")]
        public decimal DiscountPercentage { get; set; } // 10 = 10% off

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Max Discount Amount (RM)")]
        public decimal? MaxDiscountAmount { get; set; } // Optional: max RM 50 discount

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Min Order Amount (RM)")]
        public decimal? MinOrderAmount { get; set; } // Optional: min order RM 30

        [Display(Name = "Expiry Date")]
        public DateTime? ExpiryDate { get; set; }

        [Display(Name = "Max Uses (blank = unlimited)")]
        public int? MaxUses { get; set; } // Null = unlimited

        public int TimesUsed { get; set; } = 0;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}