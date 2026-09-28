namespace Shoppet_VetClinic.Models
{
    public class ClinicListingRequest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string RepresentativeName { get; set; } = string.Empty;
        public string RelationshipToClinic { get; set; } = string.Empty;
        public string ClinicName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string ClinicEmail { get; set; } = string.Empty;
        public string ServicesOffered { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public int? CreatedClinicId { get; set; }
        public int? ReviewedByUserId { get; set; }
        public string ReviewNote { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string RequesterName { get; set; } = string.Empty;
    }
}
