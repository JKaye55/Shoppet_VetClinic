namespace Shoppet_VetClinic.Models
{
    public class MarketplaceListing
    {
        public int Id { get; set; }

        public int SellerUserId { get; set; }

        public string Title { get; set; }
            = string.Empty;

        public string Category { get; set; }
            = string.Empty;

        public string ItemCondition { get; set; }
            = string.Empty;

        public decimal Price { get; set; }

        public string Description { get; set; }
            = string.Empty;

        public string Location { get; set; }
            = string.Empty;

        public string ImageUrl { get; set; }
            = string.Empty;

        public string Status { get; set; }
            = "Available";

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;


        // Display-only fields.
        // These come from UserAccounts through SQL JOIN.
        public string SellerName { get; set; }
            = string.Empty;

        public string SellerEmail { get; set; }
            = string.Empty;

        public bool IsVerifiedSeller { get; set; }
        public int OrderCount { get; set; }
        public string? LastBuyerName { get; set; }
    }
}