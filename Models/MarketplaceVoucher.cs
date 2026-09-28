namespace Shoppet_VetClinic.Models
{
    public class MarketplaceVoucher
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public int? UsedOrderId { get; set; }

        public bool IsUsable =>
            string.Equals(Status, "Active", StringComparison.OrdinalIgnoreCase)
            && ExpiresAt >= DateTime.Now
            && UsedAt is null;
    }
}
