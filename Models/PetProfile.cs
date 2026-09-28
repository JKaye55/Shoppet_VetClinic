namespace Shoppet_VetClinic.Models
{
    public class PetProfile
    {
        // =========================================================
        // DATABASE IDENTITY
        // =========================================================

        public int Id { get; set; }

        public int UserId { get; set; }


        // =========================================================
        // BASIC PET INFORMATION
        // =========================================================

        public string PetName { get; set; } =
            string.Empty;


        public string Breed { get; set; } =
            string.Empty;


        public string Species { get; set; } =
            string.Empty;


        /*
            Age is retained for compatibility with existing
            ShoppetCare pet records.

            Newer records should preferably use BirthDate,
            allowing the Digital Pet ID to calculate the
            pet's current age automatically.
        */

        public string Age { get; set; } =
            string.Empty;


        public DateTime? BirthDate { get; set; }


        public decimal? WeightKg { get; set; }


        public string Diet { get; set; } =
            string.Empty;


        // =========================================================
        // RECORD INFORMATION
        // =========================================================

        public DateTime CreatedAt { get; set; }


        // =========================================================
        // DIGITAL PET ID
        // =========================================================

        /*
            Example:

            PET-A1B2C3
        */

        public string CardId { get; set; } =
            string.Empty;


        public DateTime? CardIssuedAt { get; set; }


        /*
            Basic:
                Classic

            Premium:
                Classic
                Ocean
                Sunset
                Mint
                Royal
        */

        public string? CardTheme { get; set; }
    }
}