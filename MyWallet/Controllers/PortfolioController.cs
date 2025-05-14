using Microsoft.AspNetCore.Mvc;
using MyWallet.Models;
using MyWallet.Services;
using System;
using System.Threading.Tasks;

namespace MyWallet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PortfolioController : ControllerBase
    {
        private readonly IPortfolioService _portfolioService;

        public PortfolioController(IPortfolioService portfolioService)
        {
            _portfolioService = portfolioService;
        }

        // GET: api/portfolio/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserPortfolios(int userId)
        {
            var portfolios = await _portfolioService.GetUserPortfoliosAsync(userId);
            return Ok(portfolios);
        }

        // GET: api/portfolio/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPortfolioById(int id)
        {
            var portfolio = await _portfolioService.GetPortfolioByIdAsync(id);
            if (portfolio == null)
                return NotFound();

            return Ok(portfolio);
        }

        // POST: api/portfolio
        [HttpPost]
        public async Task<IActionResult> CreatePortfolio([FromBody] Portfolio model)
        {
            var portfolio = await _portfolioService.CreatePortfolioAsync(model);
            return CreatedAtAction(nameof(GetPortfolioById), new { id = portfolio.Id }, portfolio);
        }

        // PUT: api/portfolio
        [HttpPut]
        public async Task<IActionResult> UpdatePortfolio([FromBody] Portfolio model)
        {
            var success = await _portfolioService.UpdatePortfolioAsync(model);
            if (!success)
                return NotFound();

            return Ok("Zaktualizowano portfel.");
        }

        // DELETE: api/portfolio/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePortfolio(int id)
        {
            var success = await _portfolioService.DeletePortfolioAsync(id);
            if (!success)
                return NotFound();

            return Ok("Usunięto portfel.");
        }

        // GET: api/portfolio/{id}/value
        [HttpGet("{id}/value")]
        public async Task<IActionResult> GetPortfolioValue(int id)
        {
            var value = await _portfolioService.CalculatePortfolioValueAsync(id);
            return Ok(value);
        }

        // GET: api/portfolio/{id}/profitloss
        [HttpGet("{id}/profitloss")]
        public async Task<IActionResult> GetProfitLoss(int id)
        {
            var profitLoss = await _portfolioService.GetPortfolioProfitLossAsync(id);
            return Ok(profitLoss);
        }

        // GET: api/portfolio/{id}/invested
        [HttpGet("{id}/invested")]
        public async Task<IActionResult> GetInvestedAmount(int id)
        {
            var invested = await _portfolioService.GetInvestedAmountAsync(id);
            return Ok(invested);
        }

        // GET: api/portfolio/{id}/distribution
        [HttpGet("{id}/distribution")]
        public async Task<IActionResult> GetAssetCategoryDistribution(int id)
        {
            var distribution = await _portfolioService.GetAssetCategoryDistributionAsync(id);
            return Ok(distribution);
        }

        // POST: api/portfolio/{id}/record-history
        [HttpPost("{id}/record-history")]
        public async Task<IActionResult> RecordPortfolioHistory(int id)
        {
            var history = await _portfolioService.RecordPortfolioHistoryAsync(id);
            return Ok(history);
        }

        // GET: api/portfolio/{id}/history?start=2024-01-01&end=2024-12-31
        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetPortfolioHistory(int id, [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            var history = await _portfolioService.GetPortfolioHistoryAsync(id, start, end);
            return Ok(history);
        }
    }
}
