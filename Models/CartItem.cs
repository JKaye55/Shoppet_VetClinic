namespace Shoppet_VetClinic.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string ImageSource { get; set; } = string.Empty;
        public decimal Subtotal => Price * Quantity;
    }
}