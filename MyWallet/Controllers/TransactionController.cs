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

        public TransactionController(ITransactionService transactionService, TransactionMapper mapper)
        {
            _transactionService = transactionService;
            _mapper = mapper;
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
            var model = _mapper.ToModel(dto);
            var created = await _transactionService.CreateTransactionAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, _mapper.ToDto(created));
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] TransactionDto dto)
        {
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
    }
}
