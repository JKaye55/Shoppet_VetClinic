namespace Shoppet_VetClinic.Models
{
    public class MarketplaceOrder
    {
        public int Id { get; set; }
        public int BuyerUserId { get; set; }
        public int SellerUserId { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public string SellerName { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = "Purchase Requested";
        public decimal Subtotal { get; set; }
        public decimal VoucherDiscount { get; set; }
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<MarketplaceOrderItem> Items { get; set; } = new();
    }

    public class MarketplaceOrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ListingId { get; set; }
        public string ListingTitle { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool HasReview { get; set; }
    }
}
