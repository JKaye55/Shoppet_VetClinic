namespace Shoppet_VetClinic.Models
{
    public class MarketplaceReview
    {
        public int Id { get; set; }
        public int OrderItemId { get; set; }
        public int ListingId { get; set; }
        public int BuyerUserId { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool RewardIssued { get; set; }
    }

    public class ReviewRewardResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string VoucherCode { get; set; } = string.Empty;
        public decimal VoucherAmount { get; set; }
        public DateTime? VoucherExpiresAt { get; set; }
    }
}
