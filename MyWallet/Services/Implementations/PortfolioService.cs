using Microsoft.EntityFrameworkCore;
using MyWallet.Data;
using MyWallet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using OxyPlot.ImageSharp;
using OxyPlot.Legends;

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
                throw new KeyNotFoundException($"Portfolio with ID {portfolioId} not found");

            decimal totalValue = await CalculatePortfolioValueAsync(portfolioId);

            decimal investedAmount = portfolio.Transactions
                                         .Where(t => t.Type == TransactionType.Deposit || t.Type == TransactionType.Buy)
                                         .Sum(t => t.TotalAmount)
                                     -
                                     portfolio.Transactions
                                         .Where(t => t.Type == TransactionType.Withdrawal || t.Type == TransactionType.Sell)
                                         .Sum(t => t.TotalAmount);

            var history = new PortfolioHistory
            {
                PortfolioId = portfolioId,
                TotalValue = totalValue,
                InvestedAmount = investedAmount,
                RecordedAt = DateTime.UtcNow
            };

            _context.PortfolioHistories.Add(history);
            await _context.SaveChangesAsync();

            return history;
        }



        public async Task<IEnumerable<PortfolioHistory>> GetPortfolioHistoryAsync(int portfolioId, DateTime start, DateTime end)
        {
            start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
            end = DateTime.SpecifyKind(end, DateTimeKind.Utc);

            var history = await _context.PortfolioHistories
                .Where(h => h.PortfolioId == portfolioId &&
                            h.RecordedAt >= start &&
                            h.RecordedAt <= end)
                .ToListAsync();
            
            foreach (var h in history)
            {
                h.RecordedAt = DateTime.SpecifyKind(h.RecordedAt, DateTimeKind.Utc);
            }

            return history;
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

        public async Task<bool> PortfolioExistsAsync(int id)
        {
            return await _context.Portfolios.AnyAsync(p => p.Id == id);
        }
        
        public async Task<decimal> GetInvestedAmountAsync(int portfolioId)
        {
            var portfolio = await _context.Portfolios
                .Include(p => p.Transactions)
                .FirstOrDefaultAsync(p => p.Id == portfolioId);

            if (portfolio == null)
            {
                throw new KeyNotFoundException($"Portfolio with ID {portfolioId} not found");
            }

            decimal invested = portfolio.Transactions
                .Where(t => t.Type == TransactionType.Deposit)
                .Sum(t => t.TotalAmount);

            decimal withdrawn = portfolio.Transactions
                .Where(t => t.Type == TransactionType.Withdrawal)
                .Sum(t => t.TotalAmount);

            return invested - withdrawn;
        }
        
        public async Task<decimal> GetPortfolioProfitLossAsync(int portfolioId)
        {
            var totalValue = await CalculatePortfolioValueAsync(portfolioId);
            var investedAmount = await GetInvestedAmountAsync(portfolioId);

            return totalValue - investedAmount;
        }
        
        
        /* Generowanie wykresu */
        public async Task<byte[]> GeneratePortfolioChartWithOxyPlotAsync(int portfolioId, DateTime start, DateTime end)
        {
            var history = await GetPortfolioHistoryAsync(portfolioId, start, end);
            if (history == null || !history.Any())
                return Array.Empty<byte>();

            var plotModel = new PlotModel 
            { 
                Title = "Historia portfela",
                Background = OxyColors.White,
                PlotAreaBorderColor = OxyColors.Black,
                PlotAreaBorderThickness = new OxyThickness(1)
            };

            var dateAxis = new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "yyyy-MM-dd",
                Title = "Data",
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColors.LightGray,
                MinorGridlineStyle = LineStyle.Dot,
                MinorGridlineColor = OxyColors.LightGray
            };
            plotModel.Axes.Add(dateAxis);

            var valueAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Kwota (PLN)",
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColors.LightGray,
                MinorGridlineStyle = LineStyle.Dot,
                MinorGridlineColor = OxyColors.LightGray,
                StringFormat = "C0"
            };
            plotModel.Axes.Add(valueAxis);

            var investedSeries = new LineSeries 
            { 
                Title = "Wpłaty",
                Color = OxyColors.Blue,
                StrokeThickness = 2,
                MarkerType = MarkerType.Circle,
                MarkerSize = 3,
                MarkerFill = OxyColors.Blue
            };
            
            var valueSeries = new LineSeries 
            { 
                Title = "Aktualna wartość",
                Color = OxyColors.Green,
                StrokeThickness = 2,
                MarkerType = MarkerType.Square,
                MarkerSize = 3,
                MarkerFill = OxyColors.Green
            };

            foreach (var h in history.OrderBy(x => x.RecordedAt))
            {
                var dateValue = DateTimeAxis.ToDouble(h.RecordedAt);
                investedSeries.Points.Add(new DataPoint(dateValue, (double)h.InvestedAmount));
                valueSeries.Points.Add(new DataPoint(dateValue, (double)h.TotalValue));
            }

            plotModel.Series.Add(investedSeries);
            plotModel.Series.Add(valueSeries);
            

            using var stream = new MemoryStream();
            var pngExporter = new PngExporter(1000, 600, 300);

            // Używamy metody Export z parametrami rozmiaru i DPI
            pngExporter.Export(plotModel, stream);
            return stream.ToArray();
        }
    }
}