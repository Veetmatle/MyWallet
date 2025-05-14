namespace MyWallet.DTOs
{
    public class AssetDto
    {
        public int Id { get; set; }
        public string Symbol { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public decimal Quantity { get; set; }
        public decimal CurrentPrice { get; set; }
        public string? ImagePath { get; set; }
    }
}