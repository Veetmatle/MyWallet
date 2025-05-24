// MyWallet/Services/Implementations/TransactionService.cs
using Microsoft.EntityFrameworkCore;
using MyWallet.Data;
using MyWallet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyWallet.Services.Implementations
{
    public class TransactionService : ITransactionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAssetService _assetService;

        public TransactionService(ApplicationDbContext context, IAssetService assetService)
        {
            _context = context;
            _assetService = assetService;
        }

        public async Task<IEnumerable<Transaction>> GetPortfolioTransactionsAsync(int portfolioId)
        {
            return await _context.Transactions
                .Where(t => t.PortfolioId == portfolioId)
                .OrderByDescending(t => t.ExecutedAt)
                .ToListAsync();
        }

        public async Task<Transaction> GetTransactionByIdAsync(int id)
        {
            return await _context.Transactions
                .Include(t => t.Asset)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Transaction> CreateTransactionAsync(Transaction tx)
        {
            await _context.Transactions.AddAsync(tx);

            if (tx.AssetId is int assetId &&
                (tx.Type == TransactionType.Buy || tx.Type == TransactionType.Sell))
            {
                var asset = await _context.Assets.FindAsync(assetId);
                if (asset == null) throw new InvalidOperationException("Asset not found");

                switch (tx.Type)
                {
                    case TransactionType.Buy:
                        var totalBefore = asset.InvestedAmount;
                        var qtyBefore   = asset.Quantity;

                        asset.Quantity       += tx.Quantity;
                        asset.InvestedAmount += tx.TotalAmount;           // cena*zlecona_ilość
                        asset.AveragePurchasePrice = asset.Quantity == 0
                            ? 0
                            : asset.InvestedAmount / asset.Quantity;
                        break;

                    case TransactionType.Sell:
                        if (asset.Quantity < tx.Quantity)
                            throw new InvalidOperationException("Not enough quantity to sell");

                        asset.Quantity       -= tx.Quantity;
                        asset.InvestedAmount -= asset.AveragePurchasePrice * tx.Quantity;

                        if (asset.Quantity == 0)
                        {
                            asset.AveragePurchasePrice = 0;
                            asset.InvestedAmount       = 0;
                        }
                        break;
                }
                asset.LastUpdated = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return tx;
        }


        public async Task<bool> UpdateTransactionAsync(Transaction transaction)
        {
            // Zapisz starą transakcję do porównania
            var oldTransaction = await _context.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == transaction.Id);
            if (oldTransaction == null)
            {
                return false;
            }

            // Oblicz całkowitą kwotę transakcji
            transaction.TotalAmount = transaction.Price * transaction.Quantity;
            
            _context.Entry(transaction).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();

                // Jeśli transakcja dotyczy aktywa, zaktualizuj ilość aktywa
                if (transaction.AssetId.HasValue)
                {
                    // Wycofaj efekt starej transakcji
                    if (oldTransaction.AssetId.HasValue)
                    {
                        await ReverseTransactionEffectAsync(oldTransaction);
                    }
                    
                    // Zastosuj nową transakcję
                    await UpdateAssetBasedOnTransactionAsync(transaction);
                }

                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await TransactionExistsAsync(transaction.Id))
                {
                    return false;
                }
                throw;
            }
        }

        public async Task<bool> DeleteTransactionAsync(int id)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null)
            {
                return false;
            }

            // Jeśli transakcja dotyczy aktywa, wycofaj jej efekt
            if (transaction.AssetId.HasValue)
            {
                await ReverseTransactionEffectAsync(transaction);
            }

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByAssetAsync(int assetId)
        {
            return await _context.Transactions
                .Where(t => t.AssetId == assetId)
                .OrderByDescending(t => t.ExecutedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByDateRangeAsync(int portfolioId, DateTime startDate, DateTime endDate)
        {
            return await _context.Transactions
                .Where(t => t.PortfolioId == portfolioId && 
                           t.ExecutedAt >= startDate && 
                           t.ExecutedAt <= endDate)
                .OrderByDescending(t => t.ExecutedAt)
                .ToListAsync();
        }

        private async Task<bool> TransactionExistsAsync(int id)
        {
            return await _context.Transactions.AnyAsync(t => t.Id == id);
        }

        private async Task UpdateAssetBasedOnTransactionAsync(Transaction transaction)
        {
            var asset = await _context.Assets.FindAsync(transaction.AssetId.Value);
            if (asset == null)
            {
                return;
            }

            switch (transaction.Type)
            {
                case TransactionType.Buy:
                    asset.Quantity += transaction.Quantity;
                    break;
                case TransactionType.Sell:
                    asset.Quantity -= transaction.Quantity;
                    break;
                case TransactionType.Dividend:
                    // Dywidendy nie wpływają na ilość aktywa
                    break;
            }

            await _context.SaveChangesAsync();
        }

        private async Task ReverseTransactionEffectAsync(Transaction transaction)
        {
            var asset = await _context.Assets.FindAsync(transaction.AssetId.Value);
            if (asset == null)
            {
                return;
            }

            switch (transaction.Type)
            {
                case TransactionType.Buy:
                    asset.Quantity -= transaction.Quantity;
                    break;
                case TransactionType.Sell:
                    asset.Quantity += transaction.Quantity;
                    break;
                case TransactionType.Dividend:
                    // Dywidendy nie wpływają na ilość aktywa
                    break;
            }

            await _context.SaveChangesAsync();
        }
        
        public async Task<decimal> GetTotalInvestedAmountAsync(int portfolioId)
        {
            return await _context.Transactions
                .Where(t => t.PortfolioId == portfolioId && t.Type == TransactionType.Deposit)
                .SumAsync(t => t.TotalAmount);
        }
        
        public async Task<decimal> GetTotalWithdrawnAmountAsync(int portfolioId)
        {
            return await _context.Transactions
                .Where(t => t.PortfolioId == portfolioId && t.Type == TransactionType.Withdrawal)
                .SumAsync(t => t.TotalAmount);
        }
        
    }
}