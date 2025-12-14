namespace FoodOrderingMenu.Models
{
    public class CartItem
    {
        public int MenuItemId { get; set; }
        public string Name { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public int Qty { get; set; }

        // Drink options (optional)
        public string? Sweetness { get; set; }   // 0/30/50/75/100
        public string? IceLevel { get; set; }    // No ice / Less ice / Normal ice

        public decimal LineTotal => UnitPrice * Qty;
    }
}
