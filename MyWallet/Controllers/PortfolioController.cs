using Microsoft.AspNetCore.Mvc;
using MyWallet.DTOs;
using MyWallet.Mappers;
using MyWallet.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MyWallet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PortfolioController : ControllerBase
    {
        private readonly IPortfolioService _portfolioService;
        private readonly PortfolioMapper _portfolioMapper;

        public PortfolioController(IPortfolioService portfolioService, PortfolioMapper portfolioMapper)
        {
            _portfolioService = portfolioService;
            _portfolioMapper = portfolioMapper;
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserPortfolios(int userId)
        {
            var portfolios = await _portfolioService.GetUserPortfoliosAsync(userId);
            var dtos = portfolios.Select(_portfolioMapper.ToDto);
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPortfolioById(int id)
        {
            var portfolio = await _portfolioService.GetPortfolioByIdAsync(id);
            if (portfolio == null)
                return NotFound();

            return Ok(_portfolioMapper.ToDto(portfolio));
        }

        [HttpPost]
        public async Task<IActionResult> CreatePortfolio([FromBody] PortfolioDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            var model = _portfolioMapper.ToModel(dto);
            var created = await _portfolioService.CreatePortfolioAsync(model);
            return CreatedAtAction(nameof(GetPortfolioById), new { id = created.Id }, _portfolioMapper.ToDto(created));
        }

        [HttpPut]
        public async Task<IActionResult> UpdatePortfolio([FromBody] PortfolioDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var model = _portfolioMapper.ToModel(dto);
            var success = await _portfolioService.UpdatePortfolioAsync(model);
            if (!success)
                return NotFound();

            return Ok("Portfel zaktualizowany.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePortfolio(int id)
        {
            var success = await _portfolioService.DeletePortfolioAsync(id);
            if (!success)
                return NotFound();

            return Ok("Portfel usunięty.");
        }

        [HttpGet("{id}/value")]
        public async Task<IActionResult> GetPortfolioValue(int id)
        {
            var value = await _portfolioService.CalculatePortfolioValueAsync(id);
            return Ok(value);
        }

        [HttpGet("{id}/profitloss")]
        public async Task<IActionResult> GetProfitLoss(int id)
        {
            var result = await _portfolioService.GetPortfolioProfitLossAsync(id);
            return Ok(result);
        }

        [HttpGet("{id}/invested")]
        public async Task<IActionResult> GetInvestedAmount(int id)
        {
            var result = await _portfolioService.GetInvestedAmountAsync(id);
            return Ok(result);
        }

        [HttpGet("{id}/distribution")]
        public async Task<IActionResult> GetAssetCategoryDistribution(int id)
        {
            var result = await _portfolioService.GetAssetCategoryDistributionAsync(id);
            return Ok(result);
        }

        [HttpPost("{id}/record-history")]
        public async Task<IActionResult> RecordPortfolioHistory(int id)
        {
            var history = await _portfolioService.RecordPortfolioHistoryAsync(id);
            return Ok(history);
        }

        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetPortfolioHistory(int id, [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            var history = await _portfolioService.GetPortfolioHistoryAsync(id, start, end);
            return Ok(history);
        }
    }
}
