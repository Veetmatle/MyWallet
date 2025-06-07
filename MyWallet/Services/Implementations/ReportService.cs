using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using MyWallet.Services;
using MyWallet.Models;

namespace MyWallet.Services.Implementations
{
    public class ReportService
    {
        private readonly IEmailService        _emailService;
        private readonly IPortfolioService    _portfolioService;
        private readonly ITransactionService  _transactionService;

        public ReportService(
            IEmailService        emailService,
            IPortfolioService    portfolioService,
            ITransactionService  transactionService)
        {
            _emailService       = emailService;
            _portfolioService   = portfolioService;
            _transactionService = transactionService;
        }

        /// <summary>
        /// Wywoływane cyklicznie przez Hangfire:
        /// generuje i wysyła PDF-owe raporty dla każdego portfela każdego użytkownika.
        /// </summary>
        public async Task SendWeeklyReports()
        {
            // 1) Pobierz wszystkich użytkowników
            var users = await _portfolioService.GetAllUsersAsync();

            foreach (var user in users)
            {
                // 2) Pobierz wszystkie portfele danego użytkownika
                var portfolios = await _portfolioService.GetUserPortfoliosAsync(user.Id);

                foreach (var portfolio in portfolios)
                {
                    // 3) Ustal zakres (ostatnie 7 dni)
                    var end   = DateTime.UtcNow;
                    var start = end.AddDays(-7);

                    // 4) Wygeneruj PDF poprzez TransactionService
                    var pdfBytes = await _transactionService.GenerateReportPdfAsync(
                        portfolio.Id,
                        start,
                        end
                    );

                    // 5) Jeśli plik nie jest pusty, wyślij maila z załącznikiem
                    if (pdfBytes != null && pdfBytes.Length > 0)
                    {
                        await _emailService.SendEmailWithAttachmentAsync(
                            toEmail:         user.Email,
                            subject:         $"Tygodniowy raport portfela: {portfolio.Name}",
                            body:            $"W załączniku znajduje się raport portfela '{portfolio.Name}' za okres {start:yyyy-MM-dd}–{end:yyyy-MM-dd}.",
                            attachmentBytes: pdfBytes,
                            attachmentName:  $"raport_{portfolio.Name}_{start:yyyyMMdd}_{end:yyyyMMdd}.pdf"
                        );
                    }
                }
            }
        }
    }
}
