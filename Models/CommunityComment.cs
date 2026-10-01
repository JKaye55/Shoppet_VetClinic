namespace Shoppet_VetClinic.Models
{
    public class CommunityComment
    {
        public int Id { get; set; }
        public int? ParentCommentId {get;set;}
        public int LikeCount{get;set;}
        public int PostId { get; set; }
        public int? UserId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsGuest { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsAuthorPremium { get; set; }
        public string AuthorRole { get; set; } = string.Empty;
    }
}
