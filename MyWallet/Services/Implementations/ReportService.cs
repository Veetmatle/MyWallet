using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MyWallet.Services;
using MyWallet.Models;

namespace MyWallet.Services.Implementations
{
    public class ReportService
    {
        private readonly IEmailService        _emailService;
        private readonly IPortfolioService    _portfolioService;
        private readonly ITransactionService  _transactionService;
        private readonly ILogger<ReportService> _logger;

        public ReportService(
            IEmailService        emailService,
            IPortfolioService    portfolioService,
            ITransactionService  transactionService,
            ILogger<ReportService> logger)
        {
            _emailService       = emailService;
            _portfolioService   = portfolioService;
            _transactionService = transactionService;
            _logger             = logger;
        }

        /// <summary>
        /// Wywoływane cyklicznie przez Hangfire:
        /// generuje i wysyła PDF-owe raporty dla każdego portfela każdego użytkownika.
        /// </summary>
        public async Task SendWeeklyReports()
        {
            _logger.LogInformation("Rozpoczynam wysyłanie tygodniowych raportów...");
            Console.WriteLine("[🔄] Rozpoczynam wysyłanie tygodniowych raportów...");

            // 1) Pobierz wszystkich użytkowników
            var users = await _portfolioService.GetAllUsersAsync();

            foreach (var user in users)
            {
                _logger.LogInformation("Przetwarzam użytkownika: {Email}", user.Email);
                Console.WriteLine($"[👤] Przetwarzam użytkownika: {user.Email}");

                // 2) Pobierz wszystkie portfele danego użytkownika
                var portfolios = await _portfolioService.GetUserPortfoliosAsync(user.Id);

                foreach (var portfolio in portfolios)
                {
                    try
                    {
                        _logger.LogInformation("Generuję raport dla portfela: {PortfolioName}", portfolio.Name);
                        Console.WriteLine($"[🟡] Generuję raport dla portfela: {portfolio.Name}");

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
                            _logger.LogInformation("Wysyłam e-mail z raportem: {PortfolioName} -> {Email}", portfolio.Name, user.Email);
                            Console.WriteLine($"[📨] Wysyłam e-mail z raportem: {portfolio.Name} -> {user.Email}");

                            await _emailService.SendEmailWithAttachmentAsync(
                                toEmail:         user.Email,
                                subject:         $"Tygodniowy raport portfela: {portfolio.Name}",
                                body:            $"W załączniku znajduje się raport portfela '{portfolio.Name}' za okres {start:yyyy-MM-dd}–{end:yyyy-MM-dd}.",
                                attachmentBytes: pdfBytes,
                                attachmentName:  $"raport_{portfolio.Name}_{start:yyyyMMdd}_{end:yyyyMMdd}.pdf"
                            );

                            _logger.LogInformation("Wysłano raport dla portfela: {PortfolioName}", portfolio.Name);
                            Console.WriteLine($"[✅] Wysłano raport dla portfela: {portfolio.Name}");
                        }
                        else
                        {
                            _logger.LogWarning("Pusty raport dla portfela: {PortfolioName}, pomijam wysyłkę.", portfolio.Name);
                            Console.WriteLine($"[⚠️] Pusty raport dla portfela: {portfolio.Name}, pomijam wysyłkę.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Błąd podczas przetwarzania portfela '{PortfolioName}' użytkownika '{Email}'", portfolio.Name, user.Email);
                        Console.WriteLine($"[❌] Błąd podczas przetwarzania portfela '{portfolio.Name}' użytkownika '{user.Email}': {ex.Message}");
                    }

                    // Opcjonalnie: niewielkie opóźnienie między wysyłkami
                    await Task.Delay(500);
                }
            }

            _logger.LogInformation("Zakończono wysyłanie tygodniowych raportów.");
            Console.WriteLine("[🏁] Zakończono wysyłanie tygodniowych raportów.");
        }
    }
}
