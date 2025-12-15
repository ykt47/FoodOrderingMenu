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
        public string? CardNumber { get; set; }
        public string? CardHolderName { get; set; }
        public string? ExpiryMonth { get; set; }
        public string? ExpiryYear { get; set; }
        public string? CVV { get; set; }
        public string? EWalletPhone { get; set; }
        public string? DiscountCodeInput { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? DiscountMessage { get; set; }
        public bool IsDiscountApplied { get; set; }
        public decimal FinalTotal => GrandTotal - DiscountAmount;

    }
}
