using Microsoft.EntityFrameworkCore;
using MyWallet.Models;

namespace MyWallet.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Portfolio> Portfolios { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<AssetPriceHistory> AssetPriceHistories { get; set; }
        public DbSet<PortfolioHistory> PortfolioHistories { get; set; }
        public DbSet<ExternalApiConfig> ExternalApiConfigs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Konfiguracja indeksów
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
            
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
            
            modelBuilder.Entity<Asset>()
                .HasIndex(a => new { a.PortfolioId, a.Symbol });
            
            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.ExecutedAt);
            
            modelBuilder.Entity<AssetPriceHistory>()
                .HasIndex(aph => new { aph.AssetId, aph.RecordedAt });
            
            modelBuilder.Entity<PortfolioHistory>()
                .HasIndex(ph => new { ph.PortfolioId, ph.RecordedAt });

            // Konfiguracja relacji
            modelBuilder.Entity<Portfolio>()
                .HasOne(p => p.User)
                .WithMany(u => u.Portfolios)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<Asset>()
                .HasOne(a => a.Portfolio)
                .WithMany(p => p.Assets)
                .HasForeignKey(a => a.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Portfolio)
                .WithMany(p => p.Transactions)
                .HasForeignKey(t => t.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<AssetPriceHistory>()
                .HasOne(aph => aph.Asset)
                .WithMany(a => a.PriceHistory)
                .HasForeignKey(aph => aph.AssetId)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<PortfolioHistory>()
                .HasOne(ph => ph.Portfolio)
                .WithMany(p => p.PortfolioHistories)
                .HasForeignKey(ph => ph.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}