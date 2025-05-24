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
        private readonly ApplicationDbContext _db;
        private readonly IExternalApiService  _prices;

        public AssetService(ApplicationDbContext db, IExternalApiService prices)
        {
            _db     = db;
            _prices = prices;
        }

        /* -------------------------------------------------------------- */
        /* 1.  POBIERANIE                                                 */
        /* -------------------------------------------------------------- */

        public async Task<IEnumerable<Asset>> GetPortfolioAssetsAsync(int portfolioId) =>
            await _db.Assets.Where(a => a.PortfolioId == portfolioId).ToListAsync();

        public async Task<Asset?> GetAssetByIdAsync(int id) =>
            await _db.Assets.Include(a => a.PriceHistory).FirstOrDefaultAsync(a => a.Id == id);

        /* -------------------------------------------------------------- */
        /* 2.  TWORZENIE / DOKUPKA                                        */
        /* -------------------------------------------------------------- */

        public async Task<Asset> CreateAssetAsync(Asset asset)
        {
            asset.Symbol = asset.Symbol.ToLower();                    // standaryzacja
            asset.CurrentPrice = await _prices.GetCurrentPriceAsync(asset.Symbol, asset.Category);

            var existing = await _db.Assets.FirstOrDefaultAsync(a =>
                a.PortfolioId == asset.PortfolioId &&
                a.Symbol      == asset.Symbol      &&
                a.Category    == asset.Category);

            if (existing is null)
            {
                asset.AveragePurchasePrice = asset.CurrentPrice;
                asset.InvestedAmount       = asset.CurrentPrice * asset.Quantity;
                asset.LastUpdated          = DateTime.UtcNow;

                _db.Assets.Add(asset);
                await _db.SaveChangesAsync();
                await RecordAssetPriceHistoryAsync(asset.Id, asset.CurrentPrice);
                return asset;
            }

            /* ---- dokupka istniejącego aktywa ---- */
            decimal costBefore = existing.InvestedAmount;
            decimal costAdded  = asset.CurrentPrice * asset.Quantity;

            existing.Quantity             += asset.Quantity;
            existing.InvestedAmount       += costAdded;
            existing.AveragePurchasePrice  = existing.InvestedAmount / existing.Quantity;
            existing.LastUpdated           = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await RecordAssetPriceHistoryAsync(existing.Id, existing.CurrentPrice);
            return existing;
        }

        /* -------------------------------------------------------------- */
        /* 3.  AKTUALIZACJA NAZWY / SYMBOLU etc.                          */
        /* -------------------------------------------------------------- */

        public async Task<bool> UpdateAssetAsync(Asset updated)
        {
            var asset = await _db.Assets.FindAsync(updated.Id);
            if (asset is null) return false;

            asset.Name     = updated.Name;
            asset.Symbol   = updated.Symbol.ToLower();
            asset.Category = updated.Category;
            asset.ImagePath= updated.ImagePath;
            asset.LastUpdated = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        /* -------------------------------------------------------------- */
        /* 4.  USUWANIE                                                   */
        /* -------------------------------------------------------------- */

        public async Task<bool> DeleteAssetAsync(int id)
        {
            var asset = await _db.Assets.FindAsync(id);
            if (asset is null) return false;

            _db.Assets.Remove(asset);
            await _db.SaveChangesAsync();
            return true;
        }

        /* -------------------------------------------------------------- */
        /* 5.  WYCENY I P/L                                               */
        /* -------------------------------------------------------------- */

        public async Task<decimal> CalculateAssetCurrentValueAsync(int id)
        {
            var a = await _db.Assets.FindAsync(id) ?? throw new KeyNotFoundException();
            return a.CurrentPrice * a.Quantity;
        }

        public async Task<decimal> CalculateAssetProfitLossAsync(int id)
        {
            var a = await _db.Assets.FindAsync(id) ?? throw new KeyNotFoundException();
            var current = a.CurrentPrice * a.Quantity;
            return current - a.InvestedAmount;          // zysk brutto
        }

        /* -------------------------------------------------------------- */
        /* 6.  MASOWE ODŚWIEŻENIE CEN                                     */
        /* -------------------------------------------------------------- */

        public async Task UpdateAssetPricesAsync(int portfolioId)
        {
            var assets = await _db.Assets.Where(a => a.PortfolioId == portfolioId).ToListAsync();

            foreach (var a in assets)
            {
                var price = await _prices.GetCurrentPriceAsync(a.Symbol, a.Category);
                if (price <= 0) continue;

                a.CurrentPrice = price;
                a.LastUpdated  = DateTime.UtcNow;
                await RecordAssetPriceHistoryAsync(a.Id, price);
            }
            await _db.SaveChangesAsync();
        }

        /* -------------------------------------------------------------- */
        /* 7.  HISTORIA CEN                                               */
        /* -------------------------------------------------------------- */

        public async Task<IEnumerable<AssetPriceHistory>> GetAssetPriceHistoryAsync(
            int assetId, DateTime start, DateTime end) =>
            await _db.AssetPriceHistories
                     .Where(h => h.AssetId == assetId &&
                                 h.RecordedAt >= start && h.RecordedAt <= end)
                     .OrderBy(h => h.RecordedAt)
                     .ToListAsync();

        /* -------------------------------------------------------------- */
        /* 8.  Helpers                                                    */
        /* -------------------------------------------------------------- */

        private async Task RecordAssetPriceHistoryAsync(int assetId, decimal price)
        {
            _db.AssetPriceHistories.Add(new AssetPriceHistory
            {
                AssetId    = assetId,
                Price      = price,
                RecordedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
    }
}
