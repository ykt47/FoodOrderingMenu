using System.ComponentModel.DataAnnotations.Schema;

namespace FoodOrderingMenu.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public int MenuItemId { get; set; }
        public MenuItem? MenuItem { get; set; }

        public string Name { get; set; } = "";

        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }

        public int Qty { get; set; }

        public string? Sweetness { get; set; }
        public string? IceLevel { get; set; }

        [NotMapped]
        public decimal LineTotal => UnitPrice * Qty;
    }
}
