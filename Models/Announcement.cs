namespace Shoppet_VetClinic.Models
{
    public class Announcement
    {
        public int Id { get; set; }

        // Null = system-wide (posted by admin)
        // Set  = clinic-specific (future feature)
        public int? ClinicId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;

        // "System" | "Clinic" | "Benefit" | "Update" | "Maintenance"
        public string Category { get; set; } = "System";

        // "Low" | "Normal" | "High"
        public string Priority { get; set; } = "Normal";

        public DateTime PostedAt { get; set; } = DateTime.Now;
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Helpers
        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.Now;
        public bool IsVisible => IsActive && !IsExpired;
    }
}