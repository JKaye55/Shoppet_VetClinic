namespace Shoppet_VetClinic.Models
{
    public class PaymentReceipt
    {
        public int TransactionId { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string SubscriptionType { get; set; } = string.Empty;
        public string OfferingName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = "Paid";
        public DateTime PaidAt { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
