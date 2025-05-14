using Microsoft.EntityFrameworkCore;
using MyWallet.Data;
using MyWallet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyWallet.Services.Implementations
{
    public class PortfolioService : IPortfolioService
    {
        private readonly ApplicationDbContext _context;
        private readonly IExternalApiService _externalApiService;

        public PortfolioService(ApplicationDbContext context, IExternalApiService externalApiService)
        {
            _context = context;
            _externalApiService = externalApiService;
        }

        public async Task<IEnumerable<Portfolio>> GetUserPortfoliosAsync(int userId)
        {
            return await _context.Portfolios
                .Where(p => p.UserId == userId)
                .ToListAsync();
        }

        public async Task<Portfolio> GetPortfolioByIdAsync(int id)
        {
            return await _context.Portfolios
                .Include(p => p.Assets)
                .Include(p => p.Transactions)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Portfolio> CreatePortfolioAsync(Portfolio portfolio)
        {
            _context.Portfolios.Add(portfolio);
            await _context.SaveChangesAsync();
            return portfolio;
        }

        public async Task<bool> UpdatePortfolioAsync(Portfolio portfolio)
        {
            _context.Entry(portfolio).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await PortfolioExistsAsync(portfolio.Id))
                {
                    return false;
                }
                throw;
            }
        }

        public async Task<bool> DeletePortfolioAsync(int id)
        {
            var portfolio = await _context.Portfolios.FindAsync(id);
            if (portfolio == null)
            {
                return false;
            }

            _context.Portfolios.Remove(portfolio);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> CalculatePortfolioValueAsync(int id)
        {
            var portfolio = await _context.Portfolios
                .Include(p => p.Assets)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (portfolio == null)
            {
                throw new KeyNotFoundException($"Portfolio with ID {id} not found");
            }

            decimal totalValue = 0;

            foreach (var asset in portfolio.Assets)
            {
                totalValue += asset.CurrentPrice * asset.Quantity;
            }

            return totalValue;
        }

        public async Task<PortfolioHistory> RecordPortfolioHistoryAsync(int portfolioId)
        {
            var portfolio = await _context.Portfolios
                .Include(p => p.Assets)
                .Include(p => p.Transactions)
                .FirstOrDefaultAsync(p => p.Id == portfolioId);

            if (portfolio == null)
            {
                throw new KeyNotFoundException($"Portfolio with ID {portfolioId} not found");
            }

            decimal totalValue = await CalculatePortfolioValueAsync(portfolioId);
            
            // Obliczenie kwoty zainwestowanej (suma wpłat - suma wypłat)
            decimal investedAmount = portfolio.Transactions
                .Where(t => t.Type == TransactionType.Deposit)
                .Sum(t => t.TotalAmount)
                - portfolio.Transactions
                .Where(t => t.Type == TransactionType.Withdrawal)
                .Sum(t => t.TotalAmount);

            var portfolioHistory = new PortfolioHistory
            {
                PortfolioId = portfolioId,
                TotalValue = totalValue,
                InvestedAmount = investedAmount,
                RecordedAt = DateTime.UtcNow
            };

            _context.PortfolioHistories.Add(portfolioHistory);
            await _context.SaveChangesAsync();

            return portfolioHistory;
        }

        public async Task<IEnumerable<PortfolioHistory>> GetPortfolioHistoryAsync(int portfolioId, DateTime startDate, DateTime endDate)
        {
            return await _context.PortfolioHistories
                .Where(ph => ph.PortfolioId == portfolioId && ph.RecordedAt >= startDate && ph.RecordedAt <= endDate)
                .OrderBy(ph => ph.RecordedAt)
                .ToListAsync();
        }

        public async Task<Dictionary<string, decimal>> GetAssetCategoryDistributionAsync(int portfolioId)
        {
            var portfolio = await _context.Portfolios
                .Include(p => p.Assets)
                .FirstOrDefaultAsync(p => p.Id == portfolioId);

            if (portfolio == null)
            {
                throw new KeyNotFoundException($"Portfolio with ID {portfolioId} not found");
            }

            var distribution = new Dictionary<string, decimal>();
            decimal totalValue = await CalculatePortfolioValueAsync(portfolioId);

            if (totalValue == 0)
            {
                return distribution;
            }

            var groupedAssets = portfolio.Assets
                .GroupBy(a => a.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Value = g.Sum(a => a.CurrentPrice * a.Quantity)
                });

            foreach (var group in groupedAssets)
            {
                distribution[group.Category] = Math.Round(group.Value / totalValue * 100, 2);
            }

            return distribution;
        }

        private async Task<bool> PortfolioExistsAsync(int id)
        {
            return await _context.Portfolios.AnyAsync(p => p.Id == id);
        }
    }
}