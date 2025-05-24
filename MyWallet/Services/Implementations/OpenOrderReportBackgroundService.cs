using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;

using MyWallet.Services;
using MyWallet.Settings;
using MyWallet.DTOs;

namespace MyWallet.Services.Implementations
{
    public class OpenOrderReportBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OpenOrderReportBackgroundService> _logger;
        private readonly ReportSettings _reportSettings;
        private readonly EmailSettings _emailSettings;

        public OpenOrderReportBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<OpenOrderReportBackgroundService> logger,
            IOptions<ReportSettings> reportOptions,
            IOptions<EmailSettings> emailOptions)
        {
            _scopeFactory    = scopeFactory;
            _logger          = logger;
            _reportSettings  = reportOptions.Value;
            _emailSettings   = emailOptions.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 BackgroundService został uruchomiony!");
            Console.WriteLine("=== 🚀 BackgroundService uruchomiony ===");
            // Utwórz katalog, jeśli nie istnieje
            Directory.CreateDirectory(_reportSettings.OutputDirectory);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                    var openOrders   = await orderService.GetOpenOrdersAsync();

                    // 1) Generuj PDF
                    var fileName = Path.Combine(
                        _reportSettings.OutputDirectory,
                        $"open_orders_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                    GeneratePdf(openOrders, fileName);

                    // 2) Wyślij e-mail z załącznikiem
                    SendEmailWithAttachment(fileName);

                    _logger.LogInformation("Raport wygenerowany i wysłany: {file}", fileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Błąd podczas generowania lub wysyłania raportu");
                }

                await Task.Delay(
                    TimeSpan.FromMinutes(_reportSettings.IntervalMinutes),
                    stoppingToken);
            }
        }

        private void GeneratePdf(IEnumerable<OrderDto> orders, string outputPath)
        {
            using var doc  = new PdfDocument();
            var page       = doc.AddPage();
            var gfx        = XGraphics.FromPdfPage(page);
            var font       = new XFont("Verdana", 12);
            
            // Nagłówek
            gfx.DrawString(
                "Raport otwartych zleceń",
                font,
                XBrushes.Black,
                new XRect(0, 0, page.Width, 40),
                XStringFormats.Center);

            // Lista zleceń
            double y = 60;
            foreach (var o in orders)
            {
                gfx.DrawString(
                    $"{o.Id} | {o.Date:yyyy-MM-dd} | {o.Description}",
                    font,
                    XBrushes.Black,
                    new XPoint(40, y));
                y += 20;
                if (y > page.Height - 40)
                    break;
            }

            doc.Save(outputPath);
        }

        private void SendEmailWithAttachment(string filePath)
        {
            using var msg = new MailMessage(_emailSettings.From, _emailSettings.To)
            {
                Subject = "Raport otwartych zleceń",
                Body    = "W załączeniu najnowszy raport otwartych zleceń."
            };
            msg.Attachments.Add(new Attachment(filePath));

            using var client = new SmtpClient(
                _emailSettings.SmtpHost,
                _emailSettings.SmtpPort)
            {
                EnableSsl  = _emailSettings.UseSsl,
                Credentials = new NetworkCredential(
                    _emailSettings.Username,
                    _emailSettings.Password)
            };
            client.Send(msg);
        }
    }
}
