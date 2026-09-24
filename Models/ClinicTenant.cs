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
        public string FacilityType { get; set; } = string.Empty;
        public bool HasClinic { get; set; } = true;
        public bool HasPetShop { get; set; } = true;
        public string ContactPhone { get; set; } = string.Empty;
        public string OperatingHours { get; set; } = "8:00 AM - 6:00 PM";
        public string BrandsCarried { get; set; } = string.Empty;
        public string ServiceCapabilities { get; set; } = string.Empty;
        public string MessengerUrl { get; set; } = string.Empty;

        // Business Verification (₱150–₱300 one-time)
        public bool IsVerified { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public decimal? VerificationFee { get; set; }
        public string? VerificationReference { get; set; }
        public string VerificationStatus { get; set; } = "Unverified";
    }
}