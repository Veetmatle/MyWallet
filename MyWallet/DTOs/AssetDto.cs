namespace MyWallet.DTOs
{
    public class AssetDto
    {
        public int Id { get; set; }
        public required string Symbol { get; set; }
        public required string Name { get; set; }
        public required string Category { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal Quantity { get; set; }
        public string? ImagePath { get; set; }
    }
}