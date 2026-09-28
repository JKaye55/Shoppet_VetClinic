namespace Shoppet_VetClinic.Models
{
    public class UserAccount
    {
        // =========================================================
        // ACCOUNT
        // =========================================================

        public int Id { get; set; }


        public string FullName { get; set; }
            = string.Empty;


        public string Email { get; set; }
            = string.Empty;


        public string Password { get; set; }
            = string.Empty;


        public string Role { get; set; }
            = "Pet Owner";


        public int? ClinicId { get; set; }


        public DateTime CreatedAt { get; set; }
            = DateTime.Now;


        // =========================================================
        // CONTACT
        // =========================================================

        public string? MobileNumber { get; set; }


        // =========================================================
        // PREMIUM SUBSCRIPTION
        //
        // ShoppetCare Premium:
        // ₱150 / 3 months
        //
        // Active Premium is determined using:
        //
        // IsPremium == true
        // AND
        // PremiumActivatedAt + 3 months >= current date/time
        //
        // Do not rely on a separate PremiumStatus field.
        // =========================================================

        public bool IsPremium { get; set; }


        public DateTime? PremiumActivatedAt { get; set; }


        public string? PremiumReference { get; set; }


        // =========================================================
        // CROSS-DEVICE / FUTURE MOBILE AUTHENTICATION
        // =========================================================

        public string? ApiToken { get; set; }


        public DateTime? ApiTokenExpiresAt { get; set; }
    }
}