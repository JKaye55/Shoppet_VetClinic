namespace Shoppet_VetClinic.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? ClinicId { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = "Paid";
        public DateTime PaidAt { get; set; } = DateTime.Now;
        public string UserName { get; set; } = string.Empty;
        public string ClinicName { get; set; } = string.Empty;
    }
}
