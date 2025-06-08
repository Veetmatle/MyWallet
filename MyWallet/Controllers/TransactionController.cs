// TransactionController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyWallet.DTOs;
using MyWallet.Mappers;
using MyWallet.Models;
using MyWallet.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MyWallet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly TransactionMapper _mapper;
        private readonly IAssetService _assetService;
        private readonly ILogger<TransactionController> _logger;

        public TransactionController(
            ITransactionService transactionService,
            TransactionMapper mapper,
            IAssetService assetService,
            ILogger<TransactionController> logger)
        {
            _transactionService = transactionService;
            _mapper = mapper;
            _assetService = assetService;
            _logger = logger;
        }

        [HttpGet("portfolio/{portfolioId}")]
        public async Task<IActionResult> GetByPortfolio(int portfolioId)
        {
            _logger.LogInformation("GetByPortfolio: portfolioId={PortfolioId}", portfolioId);

            var transactions = await _transactionService.GetPortfolioTransactionsAsync(portfolioId);
            var dtos = transactions.Select(_mapper.ToDto);
            _logger.LogInformation("GetByPortfolio: zwrócono {Count} transakcji.", dtos.Count());
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogInformation("GetById: id={Id}", id);

            var transaction = await _transactionService.GetTransactionByIdAsync(id);
            if (transaction == null)
            {
                _logger.LogWarning("GetById: transakcja {Id} nie znaleziona.", id);
                return NotFound();
            }

            return Ok(_mapper.ToDto(transaction));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TransactionDto dto)
        {
            _logger.LogInformation("Create Transaction: {@Dto}", dto);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Create Transaction: niepoprawny model.");
                return BadRequest(ModelState);
            }

            var model = _mapper.ToModel(dto);
            var created = await _transactionService.CreateTransactionAsync(model);
            _logger.LogInformation("Create Transaction: utworzono transakcję Id={Id}.", created.Id);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, _mapper.ToDto(created));
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] TransactionDto dto)
        {
            _logger.LogInformation("Update Transaction: {@Dto}", dto);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Update Transaction: niepoprawny model.");
                return BadRequest(ModelState);
            }

            var model = _mapper.ToModel(dto);
            var success = await _transactionService.UpdateTransactionAsync(model);
            if (!success)
            {
                _logger.LogWarning("Update Transaction: transakcja {Id} nie istnieje.", dto.Id);
                return NotFound();
            }

            _logger.LogInformation("Update Transaction: transakcja {Id} zaktualizowana.", dto.Id);
            return Ok("Transakcja zaktualizowana.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Delete Transaction: id={Id}", id);

            var success = await _transactionService.DeleteTransactionAsync(id);
            if (!success)
            {
                _logger.LogWarning("Delete Transaction: transakcja {Id} nie istnieje.", id);
                return NotFound();
            }

            _logger.LogInformation("Delete Transaction: transakcja {Id} usunięta.", id);
            return Ok("Transakcja usunięta.");
        }

        [HttpGet("asset/{assetId}")]
        public async Task<IActionResult> GetByAsset(int assetId)
        {
            _logger.LogInformation("GetByAsset: assetId={AssetId}", assetId);

            var transactions = await _transactionService.GetTransactionsByAssetAsync(assetId);
            var dtos = transactions.Select(_mapper.ToDto);
            _logger.LogInformation("GetByAsset: zwrócono {Count} transakcji.", dtos.Count());
            return Ok(dtos);
        }

        [HttpGet("portfolio/{portfolioId}/range")]
        public async Task<IActionResult> GetByDateRange(int portfolioId, [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            _logger.LogInformation("GetByDateRange: portfolioId={PortfolioId}, start={Start}, end={End}", portfolioId, start, end);

            var transactions = await _transactionService.GetTransactionsByDateRangeAsync(portfolioId, start, end);
            var dtos = transactions.Select(_mapper.ToDto);
            _logger.LogInformation("GetByDateRange: zwrócono {Count} transakcji.", dtos.Count());
            return Ok(dtos);
        }

        [HttpGet("portfolio/{portfolioId}/invested")]
        public async Task<IActionResult> GetTotalInvested(int portfolioId)
        {
            _logger.LogInformation("GetTotalInvested: portfolioId={PortfolioId}", portfolioId);

            var total = await _transactionService.GetTotalInvestedAmountAsync(portfolioId);
            _logger.LogInformation("GetTotalInvested: kwota zainwestowana = {Total}.", total);
            return Ok(total);
        }

        [HttpGet("portfolio/{portfolioId}/withdrawn")]
        public async Task<IActionResult> GetTotalWithdrawn(int portfolioId)
        {
            _logger.LogInformation("GetTotalWithdrawn: portfolioId={PortfolioId}", portfolioId);

            var total = await _transactionService.GetTotalWithdrawnAmountAsync(portfolioId);
            _logger.LogInformation("GetTotalWithdrawn: kwota wypłacona = {Total}.", total);
            return Ok(total);
        }

        [HttpGet("portfolio/{portfolioId}/profit-loss")]
        public async Task<IActionResult> GetPortfolioProfitLoss(int portfolioId)
        {
            _logger.LogInformation("GetPortfolioProfitLoss: portfolioId={PortfolioId}", portfolioId);

            try
            {
                var assets = await _assetService.GetPortfolioAssetsAsync(portfolioId);
                var currentTotalValue = assets.Sum(a => a.CurrentPrice * a.Quantity);
                var totalInvested = await _transactionService.GetTotalInvestedAmountAsync(portfolioId);
                var profitLoss = currentTotalValue - totalInvested;
                var profitLossPercentage = totalInvested > 0
                    ? (profitLoss / totalInvested) * 100
                    : 0;

                var result = new PortfolioProfitLossDto
                {
                    TotalInvested = totalInvested,
                    CurrentValue = currentTotalValue,
                    ProfitLoss = profitLoss,
                    ProfitLossPercentage = Math.Round(profitLossPercentage, 2),
                    IsProfit = profitLoss >= 0
                };

                _logger.LogInformation("GetPortfolioProfitLoss: obliczono zysk/stratę = {ProfitLoss}, %{Percentage}.",
                    result.ProfitLoss, result.ProfitLossPercentage);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPortfolioProfitLoss: błąd podczas obliczeń dla portfolio {PortfolioId}.", portfolioId);
                return StatusCode(500, $"Błąd podczas obliczania zysku/straty: {ex.Message}");
            }
        }

        [HttpPost("sell")]
        public async Task<IActionResult> Sell([FromBody] SellAssetDto dto)
        {
            _logger.LogInformation("Sell: {@Dto}", dto);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Sell: niepoprawny model sprzedaży.");
                return BadRequest(ModelState);
            }

            try
            {
                var asset = await _assetService.SellAssetAsync(dto.AssetId, dto.Quantity, dto.Price);
                _logger.LogInformation("Sell: sprzedano assetId={AssetId}, quantity={Quantity} po price={Price}.",
                    dto.AssetId, dto.Quantity, dto.Price);
                return Ok(asset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sell: błąd podczas sprzedaży assetId={AssetId}.", dto.AssetId);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("portfolio/{portfolioId}/report")]
        public async Task<IActionResult> GetReport(
            int portfolioId,
            [FromQuery] DateTime start,
            [FromQuery] DateTime end)
        {
            _logger.LogInformation("GetReport: portfolioId={PortfolioId}, start={Start}, end={End}", portfolioId, start, end);

            if (start == default || end == default || start > end)
            {
                _logger.LogWarning("GetReport: niepoprawny zakres dat.");
                return BadRequest("Niepoprawny zakres dat.");
            }

            var transactions = await _transactionService.GetTransactionsByDateRangeAsync(portfolioId, start, end);
            var dtos = transactions.Select(_mapper.ToDto);
            _logger.LogInformation("GetReport: zwrócono {Count} transakcji raportu.", dtos.Count());
            return Ok(dtos);
        }

        [HttpGet("portfolio/{portfolioId}/profit-loss-breakdown")]
        public async Task<IActionResult> GetPortfolioProfitLossBreakdown(int portfolioId)
        {
            _logger.LogInformation("GetPortfolioProfitLossBreakdown: portfolioId={PortfolioId}", portfolioId);

            try
            {
                var assets = await _assetService.GetPortfolioAssetsAsync(portfolioId);

                var assetBreakdowns = assets.Select(asset =>
                {
                    var currentValue = asset.CurrentPrice * asset.Quantity;
                    var investedValue = asset.AveragePurchasePrice * asset.Quantity;
                    var profitLoss = currentValue - investedValue;
                    var profitLossPercentage = investedValue > 0
                        ? (profitLoss / investedValue) * 100
                        : 0;

                    return new AssetBreakdownDto
                    {
                        Symbol = asset.Symbol,
                        Quantity = asset.Quantity,
                        AverageBuyPrice = asset.AveragePurchasePrice,
                        CurrentPrice = asset.CurrentPrice,
                        CurrentValue = currentValue,
                        ProfitLoss = profitLoss,
                        ProfitLossPercent = Math.Round(profitLossPercentage, 2),
                        IsProfit = profitLoss >= 0
                    };
                }).ToList();

                var totalInvested = assetBreakdowns.Sum(x => x.AverageBuyPrice * x.Quantity);
                var totalCurrentValue = assetBreakdowns.Sum(x => x.CurrentValue);
                var totalProfitLoss = assetBreakdowns.Sum(x => x.ProfitLoss);
                var totalProfitLossPercentage = totalInvested > 0
                    ? Math.Round((totalProfitLoss / totalInvested) * 100, 2)
                    : 0;

                var result = new PortfolioProfitLossBreakdownDto
                {
                    Assets = assetBreakdowns,
                    Summary = new BreakdownSummaryDto
                    {
                        TotalAssets = assetBreakdowns.Count,
                        ProfitableAssets = assetBreakdowns.Count(x => x.IsProfit),
                        LosingAssets = assetBreakdowns.Count(x => !x.IsProfit),
                        TotalInvested = totalInvested,
                        CurrentValue = totalCurrentValue,
                        ProfitLoss = totalProfitLoss,
                        ProfitLossPercent = totalProfitLossPercentage
                    }
                };

                _logger.LogInformation(
                    "GetPortfolioProfitLossBreakdown: assets={Count}, totalPL={TotalPL}, totalPL%={PLPercentage}.",
                    assetBreakdowns.Count, totalProfitLoss, totalProfitLossPercentage);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPortfolioProfitLossBreakdown: błąd dla portfolio {PortfolioId}.", portfolioId);
                return StatusCode(500, $"Błąd podczas obliczania szczegółowego zysku/straty: {ex.Message}");
            }
        }

        [HttpGet("portfolio/{portfolioId}/report/pdf")]
        public async Task<IActionResult> GetReportPdf(int portfolioId, [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            _logger.LogInformation("GetReportPdf: portfolioId={PortfolioId}, start={Start}, end={End}", portfolioId, start, end);

            if (start == default || end == default || start > end)
            {
                _logger.LogWarning("GetReportPdf: niepoprawny zakres dat.");
                return BadRequest("Niepoprawny zakres dat.");
            }

            var pdfBytes = await _transactionService.GenerateReportPdfAsync(portfolioId, start, end);
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                _logger.LogWarning("GetReportPdf: brak danych do raportu dla portfolio {PortfolioId}.", portfolioId);
                return NotFound("Brak danych do raportu.");
            }

            _logger.LogInformation("GetReportPdf: wygenerowano PDF raportu.");
            string fileName = $"Report_Portfolio_{portfolioId}_{start:yyyyMMdd}_{end:yyyyMMdd}.pdf";

            return new FileContentResult(pdfBytes, "application/pdf")
            {
                FileDownloadName = fileName
            };
        }

        [HttpPost("portfolio/{portfolioId}/report/sendmail")]
        public async Task<IActionResult> SendReportMail(int portfolioId, [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            _logger.LogInformation("SendReportMail: portfolioId={PortfolioId}, start={Start}, end={End}", portfolioId, start, end);

            if (start == default || end == default || start > end)
            {
                _logger.LogWarning("SendReportMail: niepoprawny zakres dat.");
                return BadRequest("Niepoprawny zakres dat.");
            }

            try
            {
                await _transactionService.SendReportPdfByEmailAsync(portfolioId, start, end);
                _logger.LogInformation("SendReportMail: wysłano raport email dla portfolio {PortfolioId}.", portfolioId);
                return Ok("Raport został wysłany na Twój adres e-mail.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendReportMail: błąd podczas wysyłania maila dla portfolio {PortfolioId}.", portfolioId);
                return StatusCode(500, $"Błąd podczas wysyłania maila: {ex.Message}");
            }
        }
    }
}
