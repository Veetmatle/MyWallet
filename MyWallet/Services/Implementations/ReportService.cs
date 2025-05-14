// MyWallet/Services/Implementations/ReportService.cs
using Microsoft.Extensions.Logging;
using MyWallet.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyWallet.Services.Implementations
{
    public class ReportService : IReportService
    {
        private readonly IPortfolioService _portfolioService;
        private readonly IAssetService _assetService;
        private readonly ITransactionService _transactionService;
        private readonly ILogger<ReportService> _logger;

        public ReportService(
            IPortfolioService portfolioService,
            IAssetService assetService,
            ITransactionService transactionService,
            ILogger<ReportService> logger)
        {
            _portfolioService = portfolioService;
            _assetService = assetService;
            _transactionService = transactionService;
            _logger = logger;
            
            // Rejestracja licencji QuestPDF (Community Edition)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GeneratePortfolioSummaryReportAsync(int portfolioId)
        {
            try
            {
                var portfolio = await _portfolioService.GetPortfolioByIdAsync(portfolioId);
                if (portfolio == null)
                {
                    _logger.LogError($"Portfolio with ID {portfolioId} not found");
                    return null;
                }

                var portfolioValue = await _portfolioService.CalculatePortfolioValueAsync(portfolioId);
                var assets = portfolio.Assets.ToList();
                var assetDistribution = await _portfolioService.GetAssetCategoryDistributionAsync(portfolioId);

                // Generowanie raportu PDF
                var document = Document.Create(document =>
                {
                    document.Page(page =>
                    {
                        // Definiowanie strony
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header().Element(ComposeHeader);
                        
                        page.Content().Element(container =>
                        {
                            container.PaddingVertical(1, Unit.Centimetre);
                            
                            // Portfolio summary section
                            container.Column(column =>
                            {
                                column.Item().Text($"Portfolio Name: {portfolio.Name}")
                                    .FontSize(12).Bold();
                                    
                                column.Item().Text($"Description: {portfolio.Description}")
                                    .FontSize(10);
                                    
                                column.Item().Text($"Created: {portfolio.CreatedAt:d}")
                                    .FontSize(10);
                                    
                                column.Item().Text($"Current Value: ${portfolioValue:N2}")
                                    .FontSize(14).Bold();
                                    
                                column.Item().PaddingTop(1, Unit.Centimetre);

                                // Assets table
                                column.Item().Element(ComposeAssetsTable);
                                
                                column.Item().PaddingTop(1, Unit.Centimetre);
                                
                                // Asset distribution table
                                column.Item().Element(ComposeDistributionTable);
                            });
                        });

                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Report generated: ");
                            x.Span($"{DateTime.Now:g}");
                            x.Span(" | Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    });
                });

                using (var stream = new MemoryStream())
                {
                    document.GeneratePdf(stream);
                    return stream.ToArray();
                }

                // Local helper methods for composing the document
                void ComposeHeader(IContainer container)
                {
                    container.Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text("MyWallet - Portfolio Summary")
                                .FontSize(20).Bold();
                            column.Item().Text($"Generated on {DateTime.Now:f}")
                                .FontSize(10);
                        });
                    });
                }

                void ComposeAssetsTable(IContainer container)
                {
                    container.Table(table =>
                    {
                        // Define columns
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        // Add header
                        table.Header(header =>
                        {
                            header.Cell().Text("Symbol").Bold();
                            header.Cell().Text("Name").Bold();
                            header.Cell().Text("Category").Bold();
                            header.Cell().Text("Quantity").Bold();
                            header.Cell().Text("Price ($)").Bold();
                            header.Cell().Text("Value ($)").Bold();
                        });

                        // Add rows
                        foreach (var asset in assets)
                        {
                            var value = asset.CurrentPrice * asset.Quantity;
                            
                            table.Cell().Text(asset.Symbol);
                            table.Cell().Text(asset.Name);
                            table.Cell().Text(asset.Category);
                            table.Cell().Text($"{asset.Quantity:N8}").AlignRight();
                            table.Cell().Text($"{asset.CurrentPrice:N2}").AlignRight();
                            table.Cell().Text($"{value:N2}").AlignRight();
                        }

                        // Add total row
                        table.Cell().ColumnSpan(5).Text("Total Value").Bold().AlignRight();
                        table.Cell().Text($"{portfolioValue:N2}").Bold().AlignRight();
                    });
                }

                void ComposeDistributionTable(IContainer container)
                {
                    container.Column(column =>
                    {
                        column.Item().Text("Asset Distribution by Category")
                            .FontSize(14).Bold();
                        
                        column.Item().PaddingTop(0.5f, Unit.Centimetre).Table(table =>
                        {
                            // Define columns
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                            });

                            // Add header
                            table.Header(header =>
                            {
                                header.Cell().Text("Category").Bold();
                                header.Cell().Text("Percentage").Bold();
                            });

                            // Add rows
                            foreach (var category in assetDistribution)
                            {
                                table.Cell().Text(category.Key);
                                table.Cell().Text($"{category.Value:N2}%").AlignRight();
                            }
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating portfolio summary report for portfolio ID {portfolioId}");
                return null;
            }
        }

        public async Task<byte[]> GenerateTransactionHistoryReportAsync(int portfolioId, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var portfolio = await _portfolioService.GetPortfolioByIdAsync(portfolioId);
                if (portfolio == null)
                {
                    _logger.LogError($"Portfolio with ID {portfolioId} not found");
                    return null;
                }

                var from = startDate ?? DateTime.UtcNow.AddMonths(-1);
                var to = endDate ?? DateTime.UtcNow;
                
                var transactions = await _transactionService.GetTransactionsByDateRangeAsync(portfolioId, from, to);

                // Generowanie raportu PDF
                var document = Document.Create(document =>
                {
                    document.Page(page =>
                    {
                        // Definiowanie strony
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header().Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text("MyWallet - Transaction History Report")
                                    .FontSize(20).Bold();
                                column.Item().Text($"Portfolio: {portfolio.Name}")
                                    .FontSize(14);
                                column.Item().Text($"Period: {from:d} - {to:d}")
                                    .FontSize(12);
                            });
                        });
                        
                        page.Content().Element(container =>
                        {
                            container.PaddingVertical(1, Unit.Centimetre);
                            
                            if (!transactions.Any())
                            {
                                container.Text("No transactions found for the selected period.")
                                    .FontSize(12);
                                return;
                            }
                            
                            container.Table(table =>
                            {
                                // Define columns
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                // Add header
                                table.Header(header =>
                                {
                                    header.Cell().Text("Date").Bold();
                                    header.Cell().Text("Type").Bold();
                                    header.Cell().Text("Asset").Bold();
                                    header.Cell().Text("Quantity").Bold();
                                    header.Cell().Text("Price ($)").Bold();
                                    header.Cell().Text("Total ($)").Bold();
                                    header.Cell().Text("Notes").Bold();
                                });

                                // Add transaction rows
                                foreach (var transaction in transactions)
                                {
                                    table.Cell().Text($"{transaction.ExecutedAt:d}");
                                    table.Cell().Text($"{transaction.Type}");
                                    table.Cell().Text(transaction.AssetSymbol ?? "Cash");
                                    table.Cell().Text($"{transaction.Quantity:N8}").AlignRight();
                                    table.Cell().Text($"{transaction.Price:N2}").AlignRight();
                                    table.Cell().Text($"{transaction.TotalAmount:N2}").AlignRight();
                                    table.Cell().Text(transaction.Notes ?? "");
                                }
                            });
                        });

                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Report generated: ");
                            x.Span($"{DateTime.Now:g}");
                            x.Span(" | Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    });
                });

                using (var stream = new MemoryStream())
                {
                    document.GeneratePdf(stream);
                    return stream.ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating transaction history report for portfolio ID {portfolioId}");
                return null;
            }
        }

        public async Task<byte[]> GenerateAssetPerformanceReportAsync(int portfolioId)
        {
            try
            {
                var portfolio = await _portfolioService.GetPortfolioByIdAsync(portfolioId);
                if (portfolio == null)
                {
                    _logger.LogError($"Portfolio with ID {portfolioId} not found");
                    return null;
                }

                var assets = portfolio.Assets.ToList();

                // Generowanie raportu PDF
                var document = Document.Create(document =>
                {
                    document.Page(page =>
                    {
                        // Definiowanie strony
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header().Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text("MyWallet - Asset Performance Report")
                                    .FontSize(20).Bold();
                                column.Item().Text($"Portfolio: {portfolio.Name}")
                                    .FontSize(14);
                                column.Item().Text($"Generated on: {DateTime.Now:f}")
                                    .FontSize(10);
                            });
                        });
                        
                        page.Content().Element(container =>
                        {
                            container.PaddingVertical(1, Unit.Centimetre);
                            
                            if (!assets.Any())
                            {
                                container.Text("No assets found in this portfolio.")
                                    .FontSize(12);
                                return;
                            }
                            
                            container.Table(table =>
                            {
                                // Define columns
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(4);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                // Add header
                                table.Header(header =>
                                {
                                    header.Cell().Text("Symbol").Bold();
                                    header.Cell().Text("Name").Bold();
                                    header.Cell().Text("Category").Bold();
                                    header.Cell().Text("Quantity").Bold();
                                    header.Cell().Text("Initial ($)").Bold();
                                    header.Cell().Text("Current ($)").Bold();
                                    header.Cell().Text("Change (%)").Bold();
                                });

                                // Add asset rows
                                foreach (var asset in assets)
                                {
                                    decimal percentChange = 0;
                                    if (asset.InitialPrice > 0)
                                    {
                                        percentChange = (asset.CurrentPrice - asset.InitialPrice) / asset.InitialPrice * 100;
                                    }
                                    
                                    table.Cell().Text(asset.Symbol);
                                    table.Cell().Text(asset.Name);
                                    table.Cell().Text(asset.Category);
                                    table.Cell().Text($"{asset.Quantity:N8}").AlignRight();
                                    table.Cell().Text($"{asset.InitialPrice:N2}").AlignRight();
                                    table.Cell().Text($"{asset.CurrentPrice:N2}").AlignRight();
                                    
                                    if (percentChange > 0)
                                        table.Cell().Text($"+{percentChange:N2}%").AlignRight().FontColor(Colors.Green.Medium);
                                    else if (percentChange < 0)
                                        table.Cell().Text($"{percentChange:N2}%").AlignRight().FontColor(Colors.Red.Medium);
                                    else
                                        table.Cell().Text($"{percentChange:N2}%").AlignRight();
                                }
                            });
                        });

                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Report generated: ");
                            x.Span($"{DateTime.Now:g}");
                            x.Span(" | Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    });
                });

                using (var stream = new MemoryStream())
                {
                    document.GeneratePdf(stream);
                    return stream.ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating asset performance report for portfolio ID {portfolioId}");
                return null;
            }
        }

        public async Task<byte[]> GeneratePortfolioPerformanceChartAsync(int portfolioId, DateTime startDate, DateTime endDate)
        {
            try
            {
                // Pobierz historię portfela
                var portfolioHistory = await _portfolioService.GetPortfolioHistoryAsync(portfolioId, startDate, endDate);
                if (!portfolioHistory.Any())
                {
                    _logger.LogError($"No portfolio history found for portfolio ID {portfolioId}");
                    return null;
                }

                var portfolio = await _portfolioService.GetPortfolioByIdAsync(portfolioId);
                
                // Ten raport zawiera wykres generowany przez zewnętrzną bibliotekę
                // W rzeczywistej implementacji, użylibyśmy biblioteki do generowania wykresów jako obrazów
                // Na potrzeby przykładu, użyjemy prostego opisu wykresu
                
                var document = Document.Create(document =>
                {
                    document.Page(page =>
                    {
                        // Definiowanie strony
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header().Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text("MyWallet - Portfolio Performance Chart")
                                    .FontSize(20).Bold();
                                column.Item().Text($"Portfolio: {portfolio.Name}")
                                    .FontSize(14);
                                column.Item().Text($"Period: {startDate:d} - {endDate:d}")
                                    .FontSize(12);
                            });
                        });
                        
                        page.Content().Element(container =>
                        {
                            container.PaddingVertical(1, Unit.Centimetre);
                            
                            container.Column(column =>
                            {
                                // W rzeczywistej implementacji, tutaj byłby wstawiony obraz wykresu
                                column.Item().Height(300).Background(Colors.Grey.Lighten3)
                                    .AlignCenter().AlignMiddle()
                                    .Text("Portfolio Performance Chart\n(Implementation requires a chart generation library)")
                                    .FontSize(14);
                                
                                column.Item().PaddingTop(1, Unit.Centimetre);
                                
                                // Tabela z danymi historycznymi
                                column.Item().Table(table =>
                                {
                                    // Define columns
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });

                                    // Add header
                                    table.Header(header =>
                                    {
                                        header.Cell().Text("Date").Bold();
                                        header.Cell().Text("Portfolio Value ($)").Bold();
                                        header.Cell().Text("Invested Amount ($)").Bold();
                                    });

                                    // Add data rows
                                    foreach (var record in portfolioHistory)
                                    {
                                        table.Cell().Text($"{record.RecordedAt:d}");
                                        table.Cell().Text($"{record.TotalValue:N2}").AlignRight();
                                        table.Cell().Text($"{record.InvestedAmount:N2}").AlignRight();
                                    }
                                });
                            });
                        });

                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Report generated: ");
                            x.Span($"{DateTime.Now:g}");
                            x.Span(" | Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    });
                });

                using (var stream = new MemoryStream())
                {
                    document.GeneratePdf(stream);
                    return stream.ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating portfolio performance chart for portfolio ID {portfolioId}");
                return null;
            }
        }

        public async Task<byte[]> GenerateAssetDistributionChartAsync(int portfolioId)
        {
            try
            {
                var portfolio = await _portfolioService.GetPortfolioByIdAsync(portfolioId);
                if (portfolio == null)
                {
                    _logger.LogError($"Portfolio with ID {portfolioId} not found");
                    return null;
                }

                var assetDistribution = await _portfolioService.GetAssetCategoryDistributionAsync(portfolioId);

                // Ten raport zawiera wykres kołowy generowany przez zewnętrzną bibliotekę
                // Na potrzeby przykładu, użyjemy prostego opisu wykresu
                
                var document = Document.Create(document =>
                {
                    document.Page(page =>
                    {
                        // Definiowanie strony
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header().Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text("MyWallet - Asset Distribution Chart")
                                    .FontSize(20).Bold();
                                column.Item().Text($"Portfolio: {portfolio.Name}")
                                    .FontSize(14);
                                column.Item().Text($"Generated on: {DateTime.Now:f}")
                                    .FontSize(10);
                            });
                        });
                        
                        page.Content().Element(container =>
                        {
                            container.PaddingVertical(1, Unit.Centimetre);
                            
                            container.Column(column =>
                            {
                                // W rzeczywistej implementacji, tutaj byłby wstawiony obraz wykresu kołowego
                                column.Item().Height(300).Background(Colors.Grey.Lighten3)
                                    .AlignCenter().AlignMiddle()
                                    .Text("Asset Distribution Pie Chart\n(Implementation requires a chart generation library)")
                                    .FontSize(14);
                                
                                column.Item().PaddingTop(1, Unit.Centimetre);
                                
                                // Tabela z danymi dystrybucji
                                column.Item().Table(table =>
                                {
                                    // Define columns
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(2);
                                    });

                                    // Add header
                                    table.Header(header =>
                                    {
                                        header.Cell().Text("Category").Bold();
                                        header.Cell().Text("Percentage").Bold();
                                    });

                                    // Add data rows
                                    foreach (var category in assetDistribution)
                                    {
                                        table.Cell().Text(category.Key);
                                        table.Cell().Text($"{category.Value:N2}%").AlignRight();
                                    }
                                });
                            });
                        });

                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Report generated: ");
                            x.Span($"{DateTime.Now:g}");
                            x.Span(" | Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    });
                });

                using (var stream = new MemoryStream())
                {
                    document.GeneratePdf(stream);
                    return stream.ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating asset distribution chart for portfolio ID {portfolioId}");
                return null;
            }
        }
    }
}