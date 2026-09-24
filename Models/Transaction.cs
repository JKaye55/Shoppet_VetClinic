namespace Shoppet_VetClinic.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? ClinicId { get; set; }
        public string Type { get; set; } = string.Empty;
        // "PremiumUpgrade" | "BusinessVerification"
        public decimal Amount { get; set; }
        public string Reference { get; set; } = string.Empty;
        public DateTime PaidAt { get; set; } = DateTime.Now;
    }
}