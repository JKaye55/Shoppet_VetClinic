namespace Shoppet_VetClinic.Models
{
    public class Inquiry
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ClinicId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string PetName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string Status { get; set; } = "Submitted";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
