namespace Shoppet_VetClinic.Models
{
    public class ClinicClaimRequest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ClinicId { get; set; }
        public string RepresentativeName { get; set; } = string.Empty;
        public string RelationshipToClinic { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string SupportingDetails { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public int? ReviewedByUserId { get; set; }
        public string ReviewNote { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string RequesterName { get; set; } = string.Empty;
        public string ClinicName { get; set; } = string.Empty;
    }
}
