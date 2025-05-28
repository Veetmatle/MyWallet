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
            _db = db;
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

        public async Task<Asset> CreateAssetAsync(Asset asset, decimal? userPrice = null)
        {
            asset.Symbol = asset.Symbol.ToLower();

            if (userPrice.HasValue && userPrice.Value > 0)
                asset.CurrentPrice = userPrice.Value;
            else
                asset.CurrentPrice = await _prices.GetCurrentPriceAsync(asset.Symbol, asset.Category);

            var existing = await _db.Assets.FirstOrDefaultAsync(a =>
                a.PortfolioId == asset.PortfolioId &&
                a.Symbol == asset.Symbol &&
                a.Category == asset.Category);

            if (existing == null)
            {
                // Nowe aktywo
                asset.AveragePurchasePrice = asset.CurrentPrice;
                asset.InvestedAmount = asset.CurrentPrice * asset.Quantity;
                asset.LastUpdated = DateTime.UtcNow;

                _db.Assets.Add(asset);
                await _db.SaveChangesAsync();
                await RecordAssetPriceHistoryAsync(asset.Id, asset.CurrentPrice);

                // Zapisz transakcję zakupu BEZPOŚREDNIO do bazy
                var buyTx = new Transaction
                {
                    PortfolioId = asset.PortfolioId,
                    AssetId = asset.Id,
                    AssetSymbol = asset.Symbol,
                    Price = asset.CurrentPrice,
                    Quantity = asset.Quantity,
                    TotalAmount = asset.CurrentPrice * asset.Quantity,
                    Type = TransactionType.Buy,
                    ExecutedAt = DateTime.UtcNow,
                    Notes = ""
                };
                _db.Transactions.Add(buyTx);
                await _db.SaveChangesAsync();

                return asset;
            }

            // Dokupienie istniejącego aktywa
            decimal costAdded = asset.CurrentPrice * asset.Quantity;
            decimal oldQuantity = existing.Quantity;
            decimal oldInvestedAmount = existing.InvestedAmount;

            existing.Quantity += asset.Quantity;
            existing.InvestedAmount += costAdded;
            existing.AveragePurchasePrice = existing.InvestedAmount / existing.Quantity;
            existing.LastUpdated = DateTime.UtcNow;

            await RecordAssetPriceHistoryAsync(existing.Id, existing.CurrentPrice);

            // Zapisz transakcję dokupienia BEZPOŚREDNIO do bazy
            var buyTxExisting = new Transaction
            {
                PortfolioId = existing.PortfolioId,
                AssetId = existing.Id,
                AssetSymbol = existing.Symbol,
                Price = asset.CurrentPrice,
                Quantity = asset.Quantity,
                TotalAmount = costAdded,
                Type = TransactionType.Buy,
                ExecutedAt = DateTime.UtcNow,
                Notes = ""
            };
            _db.Transactions.Add(buyTxExisting);
            await _db.SaveChangesAsync();

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
            return current - a.InvestedAmount;
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
        
        public async Task<Asset> SellAssetAsync(int assetId, decimal quantityToSell, decimal price)
        {
            var asset = await _db.Assets.FindAsync(assetId);
            if (asset == null) throw new KeyNotFoundException("Aktywo nie znalezione");

            if (quantityToSell <= 0) throw new ArgumentException("Ilość do sprzedaży musi być większa od zera");

            if (asset.Quantity < quantityToSell)
                throw new InvalidOperationException("Nie ma wystarczającej ilości aktywa do sprzedaży");

            decimal totalAmount = price * quantityToSell;

            asset.Quantity -= quantityToSell;
            asset.InvestedAmount -= asset.AveragePurchasePrice * quantityToSell;

            if (asset.Quantity == 0)
            {
                asset.AveragePurchasePrice = 0;
                asset.InvestedAmount = 0;
            }
            asset.LastUpdated = DateTime.UtcNow;

            var sellTx = new Transaction
            {
                PortfolioId = asset.PortfolioId,
                AssetId = asset.Id,
                AssetSymbol = asset.Symbol,
                Price = price,
                Quantity = quantityToSell,
                TotalAmount = totalAmount,
                Type = TransactionType.Sell,
                ExecutedAt = DateTime.UtcNow,
                Notes = "Sprzedaż aktywa"
            };

            _db.Transactions.Add(sellTx);

            await _db.SaveChangesAsync();
            return asset;
        }

    }
    
    
}