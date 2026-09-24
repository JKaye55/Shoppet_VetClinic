namespace Shoppet_VetClinic.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;

        // Bootstrap icon class, e.g. "bi-star-fill"
        // Default to bell
        public string Icon { get; set; } = "bi-bell-fill";

        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string RelativeTime
        {
            get
            {
                var span = DateTime.Now - CreatedAt;
                if (span.TotalMinutes < 1) return "just now";
                if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
                if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
                if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
                return CreatedAt.ToString("MMM d");
            }
        }
    }
}