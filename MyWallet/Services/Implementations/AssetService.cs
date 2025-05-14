// MyWallet/Services/Implementations/AssetService.cs
using Microsoft.EntityFrameworkCore;
using MyWallet.Data;
using MyWallet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyWallet.Services.Implementations
{
    public class AssetService : IAssetService
    {
        private readonly ApplicationDbContext _context;
        private readonly IExternalApiService _externalApiService;

        public AssetService(ApplicationDbContext context, IExternalApiService externalApiService)
        {
            _context = context;
            _externalApiService = externalApiService;
        }

        public async Task<IEnumerable<Asset>> GetPortfolioAssetsAsync(int portfolioId)
        {
            return await _context.Assets
                .Where(a => a.PortfolioId == portfolioId)
                .ToListAsync();
        }

        public async Task<Asset> GetAssetByIdAsync(int id)
        {
            return await _context.Assets
                .Include(a => a.PriceHistory)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Asset> CreateAssetAsync(Asset asset)
        {
            // Pobierz aktualną cenę z API
            asset.CurrentPrice = await _externalApiService.GetCurrentPriceAsync(asset.Symbol, asset.Category);
            asset.InitialPrice = asset.CurrentPrice;
            
            _context.Assets.Add(asset);
            await _context.SaveChangesAsync();

            // Zapisz pierwszą historię ceny
            await RecordAssetPriceHistoryAsync(asset.Id, asset.CurrentPrice);

            return asset;
        }

        public async Task<bool> UpdateAssetAsync(Asset asset)
        {
            var existingAsset = await _context.Assets.FindAsync(asset.Id);
            if (existingAsset == null)
            {
                return false;
            }

            // Aktualizujemy tylko wybrane pola
            existingAsset.Name = asset.Name;
            existingAsset.Symbol = asset.Symbol;
            existingAsset.Category = asset.Category;
            existingAsset.Quantity = asset.Quantity;
            existingAsset.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<bool> DeleteAssetAsync(int id)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
            {
                return false;
            }

            _context.Assets.Remove(asset);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> CalculateAssetCurrentValueAsync(int id)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
            {
                throw new KeyNotFoundException($"Asset with ID {id} not found");
            }

            return asset.CurrentPrice * asset.Quantity;
        }

        public async Task<decimal> CalculateAssetProfitLossAsync(int id)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
            {
                throw new KeyNotFoundException($"Asset with ID {id} not found");
            }

            decimal currentValue = asset.CurrentPrice * asset.Quantity;
            decimal initialValue = asset.InitialPrice * asset.Quantity;

            return currentValue - initialValue;
        }

        public async Task UpdateAssetPricesAsync(int portfolioId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var assets = await _context.Assets
                    .Where(a => a.PortfolioId == portfolioId)
                    .ToListAsync();

                foreach (var asset in assets)
                {
                    var newPrice = await _externalApiService.GetCurrentPriceAsync(asset.Symbol, asset.Category);
                    if (newPrice > 0)
                    {
                        asset.CurrentPrice = newPrice;
                        asset.LastUpdated = DateTime.UtcNow;

                        await RecordAssetPriceHistoryAsync(asset.Id, newPrice);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw; 
            }
        }

        public async Task<IEnumerable<AssetPriceHistory>> GetAssetPriceHistoryAsync(int assetId, DateTime startDate, DateTime endDate)
        {
            return await _context.AssetPriceHistories
                .Where(h => h.AssetId == assetId && h.RecordedAt >= startDate && h.RecordedAt <= endDate)
                .OrderBy(h => h.RecordedAt)
                .ToListAsync();
        }

        private async Task<bool> AssetExistsAsync(int id)
        {
            return await _context.Assets.AnyAsync(a => a.Id == id);
        }

        private async Task RecordAssetPriceHistoryAsync(int assetId, decimal price)
        {
            var priceHistory = new AssetPriceHistory
            {
                AssetId = assetId,
                Price = price,
                RecordedAt = DateTime.UtcNow
            };

            _context.AssetPriceHistories.Add(priceHistory);
            await _context.SaveChangesAsync();
        }
    }
}