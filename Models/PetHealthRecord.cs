namespace Shoppet_VetClinic.Models
{
    public class PetHealthRecord
    {
        public int Id { get; set; }
        public int PetId { get; set; }
        public int? ClinicId { get; set; }
        public string RecordType { get; set; } = "Checkup";
        // Checkup | Vaccination | Deworming | Surgery | Medication | Other
        public string Title { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime RecordDate { get; set; } = DateTime.Now;
        public DateTime? NextDueDate { get; set; }
        public string VetName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Helpers
        public bool IsUpcoming => NextDueDate.HasValue && NextDueDate.Value > DateTime.Now;
        public bool IsOverdue => NextDueDate.HasValue && NextDueDate.Value < DateTime.Now;
    }
}