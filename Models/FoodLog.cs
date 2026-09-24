namespace Shoppet_VetClinic.Models
{
    public class FoodLog
    {
        public int Id { get; set; }
        public int PetId { get; set; }
        public string FoodName { get; set; } = string.Empty;
        public string PortionSize { get; set; } = string.Empty; // "1 cup", "200g"
        public string Notes { get; set; } = string.Empty;
        public DateTime FedAt { get; set; } = DateTime.Now;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}