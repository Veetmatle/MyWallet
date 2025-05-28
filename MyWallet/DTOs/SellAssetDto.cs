namespace MyWallet.DTOs;
using System.ComponentModel.DataAnnotations;
public class SellAssetDto
{
    [Required]
    public int AssetId { get; set; }
    
    [Range(0.00000001, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }
}
