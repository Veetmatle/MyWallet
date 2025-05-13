using System;
using System.ComponentModel.DataAnnotations;

public class User
{
    public int Id { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 5)]
    public string FullName { get; set; }

    [Required]
    public string PasswordHash { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    [Range(typeof(DateTime), "1/1/2000", "12/31/2099", ErrorMessage = "CreatedAt must be within the valid date range.")]
    public DateTime CreatedAt { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    [Range(typeof(DateTime), "1/1/2000", "12/31/2099", ErrorMessage = "UpdatedAt must be within the valid date range.")]
    public DateTime UpdatedAt { get; set; }

    public ICollection<Portfolio> Portfolios { get; set; }
}


public class Portfolio
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; }
    public ICollection<Asset> Assets { get; set; }
}

public class Asset
{
    public int Id { get; set; }

    [Required]
    public int PortfolioId { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
    public decimal Quantity { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "PriceAtPurchase must be greater than zero.")]
    public decimal PriceAtPurchase { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "CurrentPrice must be greater than zero.")]
    public decimal CurrentPrice { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime UpdatedAt { get; set; }

    public Portfolio Portfolio { get; set; }
}

public class AssetPrice
{
    public int Id { get; set; }

    [Required]
    public int AssetId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime Date { get; set; }

    public Asset Asset { get; set; }
}
