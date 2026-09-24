namespace Shoppet_VetClinic.Models
{
    public class CommunityPost
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? PetId { get; set; }
        public string Caption { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int LikeCount { get; set; }
        public DateTime PostedAt { get; set; } = DateTime.Now;

        // For display (populated when joined with UserAccounts/PetProfiles)
        public string AuthorName { get; set; } = string.Empty;
        public string PetName { get; set; } = string.Empty;
    }
}