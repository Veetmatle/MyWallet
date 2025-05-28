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

        public TransactionService(ApplicationDbContext context)
        {
            _context = context;
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
            // Oblicz TotalAmount jeśli nie jest ustawione
            if (tx.TotalAmount == 0)
            {
                tx.TotalAmount = tx.Price * tx.Quantity;
            }
            
            if (string.IsNullOrEmpty(tx.Notes))
            {
                tx.Notes = ""; // lub inna wartość domyślna
            }

            // Dodaj transakcję do kontekstu
            await _context.Transactions.AddAsync(tx);

            // NIE MODYFIKUJEMY ASSETÓW TUTAJ - to robi AssetService
            // Ta metoda powinna być używana tylko do bezpośredniego dodawania transakcji
            // (np. Deposit, Withdrawal, Dividend) lub gdy AssetService dodaje Buy/Sell

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
            if (transaction.AssetId.HasValue && 
                (transaction.Type == TransactionType.Buy || transaction.Type == TransactionType.Sell))
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
                    var costAdded = transaction.TotalAmount;
                    asset.Quantity += transaction.Quantity;
                    asset.InvestedAmount += costAdded;
                    asset.AveragePurchasePrice = asset.Quantity > 0 ? asset.InvestedAmount / asset.Quantity : 0;
                    break;
                    
                case TransactionType.Sell:
                    if (asset.Quantity < transaction.Quantity)
                        throw new InvalidOperationException("Not enough quantity to sell");
                        
                    asset.Quantity -= transaction.Quantity;
                    asset.InvestedAmount -= asset.AveragePurchasePrice * transaction.Quantity;
                    
                    if (asset.Quantity == 0)
                    {
                        asset.AveragePurchasePrice = 0;
                        asset.InvestedAmount = 0;
                    }
                    break;
                    
                case TransactionType.Dividend:
                    // Dywidendy nie wpływają na ilość aktywa
                    break;
            }

            asset.LastUpdated = DateTime.UtcNow;
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
                    // Wycofaj zakup
                    asset.Quantity -= transaction.Quantity;
                    asset.InvestedAmount -= transaction.TotalAmount;
                    asset.AveragePurchasePrice = asset.Quantity > 0 ? asset.InvestedAmount / asset.Quantity : 0;
                    break;
                    
                case TransactionType.Sell:
                    // Wycofaj sprzedaż
                    asset.Quantity += transaction.Quantity;
                    asset.InvestedAmount += asset.AveragePurchasePrice * transaction.Quantity;
                    break;
                    
                case TransactionType.Dividend:
                    // Dywidendy nie wpływają na ilość aktywa
                    break;
            }

            asset.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        
        public async Task<decimal> GetTotalInvestedAmountAsync(int portfolioId)
        {
            return await _context.Transactions
                .Where(t => t.PortfolioId == portfolioId && 
                           (t.Type == TransactionType.Deposit || t.Type == TransactionType.Buy))
                .SumAsync(t => t.TotalAmount);
        }
        
        public async Task<decimal> GetTotalWithdrawnAmountAsync(int portfolioId)
        {
            return await _context.Transactions
                .Where(t => t.PortfolioId == portfolioId && 
                           (t.Type == TransactionType.Withdrawal || t.Type == TransactionType.Sell))
                .SumAsync(t => t.TotalAmount);
        }
    }
}