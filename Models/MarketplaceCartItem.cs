namespace Shoppet_VetClinic.Models
{
    public class MarketplaceCartItem
    {
        public int ListingId { get; set; }
        public int SellerUserId { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Status { get; set; } = "Available";
        public decimal Subtotal => Price;
    }
}
