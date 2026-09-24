namespace Shoppet_VetClinic.Models
{
    public class UserAccount
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Pet Owner";
        public int? ClinicId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Premium
        public bool IsPremium { get; set; }
        public DateTime? PremiumActivatedAt { get; set; }
        public string? PremiumReference { get; set; }

        // Cross-device token (for future mobile)
        public string? ApiToken { get; set; }              // ← ADDED BACK
        public DateTime? ApiTokenExpiresAt { get; set; }   // ← ADDED BACK
    }
}