namespace Shoppet_VetClinic.Models
{
    public class ClinicTenant
    {
        public int Id { get; set; }
        public string ClinicName { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PrcLicenseNo { get; set; } = string.Empty;
        public string PtrNumber { get; set; } = string.Empty;
        public string SubscriptionPlan { get; set; } = string.Empty;
    }
}