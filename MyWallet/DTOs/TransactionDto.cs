namespace MyWallet.DTOs;

public class TransactionDto
{
    public int Id { get; set; }
    public string AssetSymbol { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime ExecutedAt { get; set; }
    public string Type { get; set; }
}
