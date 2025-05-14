using Microsoft.AspNetCore.Mvc;
using MyWallet.Models;
using MyWallet.Services;
using System;
using System.Threading.Tasks;

namespace MyWallet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        // GET: api/transaction/portfolio/{portfolioId}
        [HttpGet("portfolio/{portfolioId}")]
        public async Task<IActionResult> GetByPortfolio(int portfolioId)
        {
            var transactions = await _transactionService.GetPortfolioTransactionsAsync(portfolioId);
            return Ok(transactions);
        }

        // GET: api/transaction/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var transaction = await _transactionService.GetTransactionByIdAsync(id);
            if (transaction == null)
                return NotFound();

            return Ok(transaction);
        }

        // POST: api/transaction
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Transaction model)
        {
            var created = await _transactionService.CreateTransactionAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/transaction
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] Transaction model)
        {
            var success = await _transactionService.UpdateTransactionAsync(model);
            if (!success)
                return NotFound();

            return Ok("Transakcja została zaktualizowana.");
        }

        // DELETE: api/transaction/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _transactionService.DeleteTransactionAsync(id);
            if (!success)
                return NotFound();

            return Ok("Transakcja została usunięta.");
        }

        // GET: api/transaction/asset/{assetId}
        [HttpGet("asset/{assetId}")]
        public async Task<IActionResult> GetByAsset(int assetId)
        {
            var transactions = await _transactionService.GetTransactionsByAssetAsync(assetId);
            return Ok(transactions);
        }

        // GET: api/transaction/portfolio/{portfolioId}/range?start=2024-01-01&end=2024-12-31
        [HttpGet("portfolio/{portfolioId}/range")]
        public async Task<IActionResult> GetByDateRange(int portfolioId, [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            var transactions = await _transactionService.GetTransactionsByDateRangeAsync(portfolioId, start, end);
            return Ok(transactions);
        }

        // GET: api/transaction/portfolio/{portfolioId}/invested
        [HttpGet("portfolio/{portfolioId}/invested")]
        public async Task<IActionResult> GetTotalInvested(int portfolioId)
        {
            var total = await _transactionService.GetTotalInvestedAmountAsync(portfolioId);
            return Ok(total);
        }

        // GET: api/transaction/portfolio/{portfolioId}/withdrawn
        [HttpGet("portfolio/{portfolioId}/withdrawn")]
        public async Task<IActionResult> GetTotalWithdrawn(int portfolioId)
        {
            var total = await _transactionService.GetTotalWithdrawnAmountAsync(portfolioId);
            return Ok(total);
        }
    }
}
