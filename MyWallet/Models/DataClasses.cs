using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyWallet.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Username { get; set; }
        
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }
        
        [Required]
        public string PasswordHash { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigational properties
        public virtual ICollection<Portfolio> Portfolios { get; set; } = new List<Portfolio>();
    }

    public class Portfolio
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [StringLength(500)]
        public string Description { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigational properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
        
        public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
        
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        
        public virtual ICollection<PortfolioHistory> PortfolioHistories { get; set; } = new List<PortfolioHistory>();
    }

    public class Asset
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Symbol { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Category { get; set; }
        
        [Column(TypeName = "decimal(18, 8)")]
        public decimal InitialPrice { get; set; }
        
        [Column(TypeName = "decimal(18, 8)")]
        public decimal CurrentPrice { get; set; }
        
        [Column(TypeName = "decimal(18, 8)")]
        public decimal Quantity { get; set; }
        
        [Required]
        public int PortfolioId { get; set; }
        
        public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;
        
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        
        // Navigational properties
        [ForeignKey("PortfolioId")]
        public virtual Portfolio Portfolio { get; set; }
        
        [Column(TypeName = "decimal(18,8)")]
        public decimal AveragePurchasePrice { get; set; }      // nowy – średni koszt sztuki

        [Column(TypeName = "decimal(18,8)")]
        public decimal InvestedAmount { get; set; }            // łączny koszt nabycia (opcjonalnie)
        
        public virtual ICollection<AssetPriceHistory> PriceHistory { get; set; } = new List<AssetPriceHistory>();
        
        public string? ImagePath { get; set; }
    }

    public enum TransactionType
    {
        Buy,
        Sell,
        Deposit,
        Withdrawal,
        Dividend
    }

    public class Transaction
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public TransactionType Type { get; set; }
        
        [Required]
        public int PortfolioId { get; set; }
        
        public int? AssetId { get; set; }
        
        [StringLength(20)]
        public string AssetSymbol { get; set; }
        
        [Column(TypeName = "decimal(18, 8)")]
        public decimal Price { get; set; }
        
        [Column(TypeName = "decimal(18, 8)")]
        public decimal Quantity { get; set; }
        
        [Column(TypeName = "decimal(18, 8)")]
        public decimal TotalAmount { get; set; }
        
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

        [StringLength(500)] public string Notes { get; set; } = "";
        
        // Navigational properties
        [ForeignKey("PortfolioId")]
        public virtual Portfolio Portfolio { get; set; }
        
        [ForeignKey("AssetId")]
        public virtual Asset Asset { get; set; }
    }

    public class AssetPriceHistory
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int AssetId { get; set; }
        
        [Column(TypeName = "decimal(18, 8)")]
        public decimal Price { get; set; }
        
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
        
        // Navigational properties
        [ForeignKey("AssetId")]
        public virtual Asset Asset { get; set; }
    }

    public class PortfolioHistory
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int PortfolioId { get; set; }
        
        [Column(TypeName = "decimal(18, 8)")]
        public decimal TotalValue { get; set; }
        
        [Column(TypeName = "decimal(18, 8)")]
        public decimal InvestedAmount { get; set; }
        
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
        
        // Navigational properties
        [ForeignKey("PortfolioId")]
        public virtual Portfolio Portfolio { get; set; }
    }

    public class ExternalApiConfig
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        
        [StringLength(100)]
        public string ApiKey { get; set; }
        
        [Required]
        [StringLength(255)]
        public string BaseUrl { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
}