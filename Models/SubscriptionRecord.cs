namespace Shoppet_VetClinic.Models
{
    public class SubscriptionRecord
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? ClinicId { get; set; }
        public string SubscriptionType { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public string Reference { get; set; } = string.Empty;
        public DateTime StartsAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string ClinicName { get; set; } = string.Empty;

        public bool IsActive =>
            string.Equals(Status, "Active", StringComparison.OrdinalIgnoreCase)
            && StartsAt <= DateTime.Now
            && ExpiresAt >= DateTime.Now;
    }
}
