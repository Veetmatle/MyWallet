namespace MyWallet.DTOs;

public class TransactionDto
{
    public int Id { get; set; }
    public required string AssetSymbol { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public required string Type { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime ExecutedAt { get; set; }
}
