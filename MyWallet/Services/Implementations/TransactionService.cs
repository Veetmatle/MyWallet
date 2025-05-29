using Microsoft.EntityFrameworkCore;
using MyWallet.Data;
using MyWallet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using System.Globalization;

namespace MyWallet.Services.Implementations
{
    public class TransactionService : ITransactionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IPortfolioService _portfolioService;
        private readonly IUserService _userService;

        public TransactionService(ApplicationDbContext context,
            IEmailService emailService,
            IPortfolioService portfolioService,
            IUserService userService)
        {
            _context = context;
            _emailService = emailService;
            _portfolioService = portfolioService;
            _userService = userService;
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
            if (tx.TotalAmount == 0)
            {
                tx.TotalAmount = tx.Price * tx.Quantity;
            }
            
            if (string.IsNullOrEmpty(tx.Notes))
            {
                tx.Notes = "";
            }
            
            if (tx.Type == TransactionType.Buy || 
                tx.Type == TransactionType.Sell || 
                tx.Type == TransactionType.Deposit || 
                tx.Type == TransactionType.Withdrawal)
            {
                await _portfolioService.RecordPortfolioHistoryAsync(tx.PortfolioId);
            }

            await _context.Transactions.AddAsync(tx);
            await _context.SaveChangesAsync();

            // Aktualizacja historii portfela po dodaniu transakcji
            await _portfolioService.RecordPortfolioHistoryAsync(tx.PortfolioId);

            return tx;
        }

        public async Task<bool> UpdateTransactionAsync(Transaction transaction)
        {
            var oldTransaction = await _context.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == transaction.Id);
            if (oldTransaction == null)
            {
                return false;
            }

            transaction.TotalAmount = transaction.Price * transaction.Quantity;
            _context.Entry(transaction).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();

                // Aktualizacja historii portfela po aktualizacji transakcji
                await _portfolioService.RecordPortfolioHistoryAsync(transaction.PortfolioId);

                if (transaction.AssetId.HasValue)
                {
                    if (oldTransaction.AssetId.HasValue)
                    {
                        await ReverseTransactionEffectAsync(oldTransaction);
                    }
                    
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

            if (transaction.AssetId.HasValue && 
                (transaction.Type == TransactionType.Buy || transaction.Type == TransactionType.Sell))
            {
                await ReverseTransactionEffectAsync(transaction);
            }

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();

            // Aktualizacja historii portfela po usunięciu transakcji
            await _portfolioService.RecordPortfolioHistoryAsync(transaction.PortfolioId);

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
                    asset.Quantity -= transaction.Quantity;
                    asset.InvestedAmount -= transaction.TotalAmount;
                    asset.AveragePurchasePrice = asset.Quantity > 0 ? asset.InvestedAmount / asset.Quantity : 0;
                    break;
                    
                case TransactionType.Sell:
                    asset.Quantity += transaction.Quantity;
                    asset.InvestedAmount += asset.AveragePurchasePrice * transaction.Quantity;
                    break;
                    
                case TransactionType.Dividend:
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
        
        public async Task<byte[]> GenerateReportPdfAsync(int portfolioId, DateTime start, DateTime end)
        {
            var transactions = await GetTransactionsByDateRangeAsync(portfolioId, start, end);
            if (transactions == null || !transactions.Any())
                return Array.Empty<byte>();

            var portfolio = await _portfolioService.GetPortfolioByIdAsync(portfolioId);

            using var document = new PdfDocument();
            document.Info.Title = $"Raport transakcji portfela {portfolio?.Name ?? portfolioId.ToString()}";

            PdfPage page = null;
            XGraphics gfx = null;
            XFont titleFont = new XFont("Verdana", 16, XFontStyle.Bold);
            XFont headerFont = new XFont("Verdana", 12, XFontStyle.Bold);
            XFont contentFont = new XFont("Verdana", 10, XFontStyle.Regular);

            int margin = 40;
            int yPoint = 0;
            int lineHeight = 20;
            int pageHeightLimit = 800;

            void AddPageAndHeader()
            {
                page = document.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                yPoint = margin;

                string title = $"Raport transakcji portfela: {portfolio?.Name ?? portfolioId.ToString()}";
                gfx.DrawString(title, titleFont, XBrushes.Black, new XRect(0, yPoint, page.Width, lineHeight), XStringFormats.TopCenter);
                yPoint += lineHeight + 10;

                string period = $"Okres: {start:yyyy-MM-dd} - {end:yyyy-MM-dd}";
                gfx.DrawString(period, contentFont, XBrushes.Black, new XRect(margin, yPoint, page.Width, lineHeight), XStringFormats.TopLeft);
                yPoint += lineHeight + 15;

                gfx.DrawString("Data", headerFont, XBrushes.Black, margin, yPoint);
                gfx.DrawString("Aktywo", headerFont, XBrushes.Black, margin + 70, yPoint);
                gfx.DrawString("Typ", headerFont, XBrushes.Black, margin + 140, yPoint);
                gfx.DrawString("Ilość", headerFont, XBrushes.Black, margin + 200, yPoint);
                gfx.DrawString("Cena", headerFont, XBrushes.Black, margin + 260, yPoint);
                gfx.DrawString("Kwota", headerFont, XBrushes.Black, margin + 320, yPoint);
                gfx.DrawString("Notatki", headerFont, XBrushes.Black, margin + 380, yPoint);

                yPoint += lineHeight;
            }

            AddPageAndHeader();

            foreach (var tx in transactions)
            {
                if (yPoint + lineHeight > pageHeightLimit)
                {
                    AddPageAndHeader();
                }

                gfx.DrawString(tx.ExecutedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), contentFont, XBrushes.Black, margin, yPoint);
                gfx.DrawString(tx.AssetSymbol.ToUpperInvariant(), contentFont, XBrushes.Black, margin + 70, yPoint);
                gfx.DrawString(tx.Type != null ? tx.Type.ToString() : "-", contentFont, XBrushes.Black, margin + 140, yPoint);
                gfx.DrawString(tx.Quantity.ToString("0.######"), contentFont, XBrushes.Black, margin + 200, yPoint);
                gfx.DrawString($"${tx.Price:0.00}", contentFont, XBrushes.Black, margin + 260, yPoint);
                gfx.DrawString($"${tx.TotalAmount:0.00}", contentFont, XBrushes.Black, margin + 320, yPoint);
                gfx.DrawString(tx.Notes ?? "-", contentFont, XBrushes.Black, margin + 380, yPoint);

                yPoint += lineHeight;
            }

            using var stream = new MemoryStream();
            document.Save(stream, false);
            return stream.ToArray();
        }
        
        public async Task SendReportPdfByEmailAsync(int portfolioId, DateTime start, DateTime end)
        {
            try
            {
                Console.WriteLine($"Rozpoczynam wysyłanie raportu dla portfela {portfolioId}");

                var pdfBytes = await GenerateReportPdfAsync(portfolioId, start, end);
                if (pdfBytes == null || pdfBytes.Length == 0)
                    throw new Exception("Brak danych do raportu.");

                Console.WriteLine($"PDF wygenerowany, rozmiar: {pdfBytes.Length} bajtów");

                var portfolio = await _portfolioService.GetPortfolioByIdAsync(portfolioId);
                Console.WriteLine($"Portfolio: {portfolio?.Name ?? "brak nazwy"}");

                var userEmail = await _userService.GetUserEmailByPortfolioIdAsync(portfolioId);
                Console.WriteLine($"Email użytkownika: {userEmail ?? "brak emaila"}");

                if (string.IsNullOrEmpty(userEmail))
                    throw new Exception("Nie znaleziono adresu email użytkownika.");

                string subject = $"Raport portfela {portfolio?.Name} z dnia {DateTime.Now:yyyy-MM-dd}";
                string body = $"Raport transakcji za okres {start:yyyy-MM-dd} - {end:yyyy-MM-dd}.\n\nMiłego dnia :)";
                string fileName = $"Report_{portfolio?.Name}_{start:yyyyMMdd}_{end:yyyyMMdd}.pdf";

                Console.WriteLine($"Wysyłam email do: {userEmail}");
                Console.WriteLine($"Temat: {subject}");
                Console.WriteLine($"Nazwa załącznika: {fileName}");

                await _emailService.SendEmailWithAttachmentAsync(userEmail, subject, body, pdfBytes, fileName);

                Console.WriteLine("Email wysłany pomyślnie!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd wysyłania emaila: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}
