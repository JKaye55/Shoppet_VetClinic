namespace Shoppet_VetClinic.Models
{
    public class SubscriptionOffering
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int DurationMonths { get; set; }
    }
}
