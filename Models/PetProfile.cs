namespace Shoppet_VetClinic.Models
{
    public class PetProfile
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string PetName { get; set; } = string.Empty;
        public string Breed { get; set; } = string.Empty;
        public string Species { get; set; } = "Dog";
        public string Age { get; set; } = string.Empty;
        public decimal? WeightKg { get; set; }
        public string Diet { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Pet ID Virtual Card
        public string CardId { get; set; } = string.Empty;
        public DateTime? CardIssuedAt { get; set; }
        public string? CardTheme { get; set; }   // ← ADDED BACK
    }
}