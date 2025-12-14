namespace FoodOrderingMenu.Models
{
    public class CheckoutViewModel
    {
        public List<CartItem> Items { get; set; } = new();

        public decimal Subtotal { get; set; }
        public decimal ServiceTax { get; set; } // 10%
        public decimal SST { get; set; }        // 6%
        public decimal GrandTotal { get; set; }

        public string PaymentMethod { get; set; } = "PayAtCounter"; // PayAtCounter/Card/EWallet
        public string? EWalletProvider { get; set; } // TNG/GrabPay/Boost/ShopeePay
    }
}
