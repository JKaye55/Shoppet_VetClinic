namespace Shoppet_VetClinic.Models
{
    public class UserAccount
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Pet Owner";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}