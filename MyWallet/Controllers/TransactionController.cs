using Microsoft.AspNetCore.Mvc;
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

        public TransactionController(
            ITransactionService transactionService, 
            TransactionMapper mapper,
            IAssetService assetService)
        {
            _transactionService = transactionService;
            _mapper = mapper;
            _assetService = assetService;
        }

        [HttpGet("portfolio/{portfolioId}")]
        public async Task<IActionResult> GetByPortfolio(int portfolioId)
        {
            var transactions = await _transactionService.GetPortfolioTransactionsAsync(portfolioId);
            var dtos = transactions.Select(_mapper.ToDto);
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var transaction = await _transactionService.GetTransactionByIdAsync(id);
            if (transaction == null)
                return NotFound();

            return Ok(_mapper.ToDto(transaction));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TransactionDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            var model = _mapper.ToModel(dto);
            var created = await _transactionService.CreateTransactionAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, _mapper.ToDto(created));
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] TransactionDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            var model = _mapper.ToModel(dto);
            var success = await _transactionService.UpdateTransactionAsync(model);
            if (!success)
                return NotFound();

            return Ok("Transakcja zaktualizowana.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _transactionService.DeleteTransactionAsync(id);
            if (!success)
                return NotFound();

            return Ok("Transakcja usunięta.");
        }

        [HttpGet("asset/{assetId}")]
        public async Task<IActionResult> GetByAsset(int assetId)
        {
            var transactions = await _transactionService.GetTransactionsByAssetAsync(assetId);
            return Ok(transactions.Select(_mapper.ToDto));
        }

        [HttpGet("portfolio/{portfolioId}/range")]
        public async Task<IActionResult> GetByDateRange(int portfolioId, [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            var transactions = await _transactionService.GetTransactionsByDateRangeAsync(portfolioId, start, end);
            return Ok(transactions.Select(_mapper.ToDto));
        }

        [HttpGet("portfolio/{portfolioId}/invested")]
        public async Task<IActionResult> GetTotalInvested(int portfolioId)
        {
            var total = await _transactionService.GetTotalInvestedAmountAsync(portfolioId);
            return Ok(total);
        }

        [HttpGet("portfolio/{portfolioId}/withdrawn")]
        public async Task<IActionResult> GetTotalWithdrawn(int portfolioId)
        {
            var total = await _transactionService.GetTotalWithdrawnAmountAsync(portfolioId);
            return Ok(total);
        }
        
        [HttpGet("portfolio/{portfolioId}/profit-loss")]
        public async Task<IActionResult> GetPortfolioProfitLoss(int portfolioId)
        {
            try
            {
                var assets = await _assetService.GetPortfolioAssetsAsync(portfolioId);
                var currentTotalValue = assets.Sum(a => a.CurrentPrice * a.Quantity);
                var totalInvested = await _transactionService.GetTotalInvestedAmountAsync(portfolioId);
                var profitLoss = currentTotalValue - totalInvested;
                var profitLossPercentage = totalInvested > 0 ? (profitLoss / totalInvested) * 100 : 0;

                var result = new PortfolioProfitLossDto
                {
                    TotalInvested = totalInvested,
                    CurrentValue = currentTotalValue,
                    ProfitLoss = profitLoss,
                    ProfitLossPercentage = Math.Round(profitLossPercentage, 2),
                    IsProfit = profitLoss >= 0
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas obliczania zysku/straty: {ex.Message}");
            }
        }
        
        [HttpPost("sell")]
        public async Task<IActionResult> Sell([FromBody] SellAssetDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var asset = await _assetService.SellAssetAsync(dto.AssetId, dto.Quantity, dto.Price);
                return Ok(asset);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        
        [HttpGet("portfolio/{portfolioId}/report")]
        public async Task<IActionResult> GetReport(
            int portfolioId, 
            [FromQuery] DateTime start, 
            [FromQuery] DateTime end)
        {
            if (start == default || end == default || start > end)
                return BadRequest("Niepoprawny zakres dat.");
            
            start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
            end = DateTime.SpecifyKind(end, DateTimeKind.Utc);

            var transactions = await _transactionService.GetTransactionsByDateRangeAsync(portfolioId, start, end);
            var dtos = transactions.Select(_mapper.ToDto);
            return Ok(dtos);
        }



        
        [HttpGet("portfolio/{portfolioId}/profit-loss-breakdown")]
        public async Task<IActionResult> GetPortfolioProfitLossBreakdown(int portfolioId)
        {
            try
            {
                var assets = await _assetService.GetPortfolioAssetsAsync(portfolioId);
                
                var assetBreakdowns = assets.Select(asset => {
                    var currentValue = asset.CurrentPrice * asset.Quantity;
                    var investedValue = asset.AveragePurchasePrice * asset.Quantity;
                    var profitLoss = currentValue - investedValue;
                    var profitLossPercentage = investedValue > 0 ? (profitLoss / investedValue) * 100 : 0;

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
                }).ToList(); // Convert to List here

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

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas obliczania szczegółowego zysku/straty: {ex.Message}");
            }
        }
        
        [HttpGet("portfolio/{portfolioId}/report/pdf")]
        public async Task<IActionResult> GetReportPdf(int portfolioId, [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            if (start == default || end == default || start > end)
                return BadRequest("Niepoprawny zakres dat.");

            var pdfBytes = await _transactionService.GenerateReportPdfAsync(portfolioId, start, end);
            if (pdfBytes == null || pdfBytes.Length == 0)
                return NotFound("Brak danych do raportu.");

            string fileName = $"Report_Portfolio_{portfolioId}_{start:yyyyMMdd}_{end:yyyyMMdd}.pdf";
            
            return new FileContentResult(pdfBytes, "application/pdf")
            {
                FileDownloadName = fileName
            };
        }


    }
}