using Microsoft.AspNetCore.Mvc;
using MyWallet.Models;
using MyWallet.Services;
using System;
using System.Threading.Tasks;

namespace MyWallet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssetController : ControllerBase
    {
        private readonly IAssetService _assetService;

        public AssetController(IAssetService assetService)
        {
            _assetService = assetService;
        }

        // GET: api/asset/portfolio/{portfolioId}
        [HttpGet("portfolio/{portfolioId}")]
        public async Task<IActionResult> GetAssetsByPortfolio(int portfolioId)
        {
            var assets = await _assetService.GetPortfolioAssetsAsync(portfolioId);
            return Ok(assets);
        }

        // GET: api/asset/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAssetById(int id)
        {
            var asset = await _assetService.GetAssetByIdAsync(id);
            if (asset == null)
                return NotFound();

            return Ok(asset);
        }

        // POST: api/asset
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Asset model)
        {
            var created = await _assetService.CreateAssetAsync(model);
            return CreatedAtAction(nameof(GetAssetById), new { id = created.Id }, created);
        }

        // PUT: api/asset
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] Asset model)
        {
            var success = await _assetService.UpdateAssetAsync(model);
            if (!success)
                return NotFound();

            return Ok("Aktywo zostało zaktualizowane.");
        }

        // DELETE: api/asset/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _assetService.DeleteAssetAsync(id);
            if (!success)
                return NotFound();

            return Ok("Aktywo zostało usunięte.");
        }

        // PUT: api/asset/portfolio/{portfolioId}/update-prices
        [HttpPut("portfolio/{portfolioId}/update-prices")]
        public async Task<IActionResult> UpdateAssetPrices(int portfolioId)
        {
            await _assetService.UpdateAssetPricesAsync(portfolioId);
            return Ok("Ceny aktywów zaktualizowane.");
        }

        // GET: api/asset/{id}/value
        [HttpGet("{id}/value")]
        public async Task<IActionResult> GetAssetCurrentValue(int id)
        {
            var value = await _assetService.CalculateAssetCurrentValueAsync(id);
            return Ok(value);
        }

        // GET: api/asset/{id}/profitloss
        [HttpGet("{id}/profitloss")]
        public async Task<IActionResult> GetAssetProfitLoss(int id)
        {
            var profitLoss = await _assetService.CalculateAssetProfitLossAsync(id);
            return Ok(profitLoss);
        }

        // GET: api/asset/{id}/history?start=2024-01-01&end=2024-12-31
        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetAssetPriceHistory(int id, [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            var history = await _assetService.GetAssetPriceHistoryAsync(id, start, end);
            return Ok(history);
        }
        
        [HttpPost("{id}/upload-image")]
        public async Task<IActionResult> UploadImage(int id, [FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Plik nie został przesłany.");

            var asset = await _assetService.GetAssetByIdAsync(id);
            if (asset == null)
                return NotFound("Aktywo nie istnieje.");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/assets");
            Directory.CreateDirectory(uploadsFolder);

            var extension = Path.GetExtension(file.FileName);
            var fileName = $"asset_{id}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Aktualizacja modelu
            asset.ImagePath = $"uploads/assets/{fileName}";
            await _assetService.UpdateAssetAsync(asset);

            return Ok(new { imageUrl = asset.ImagePath });
        }
    }
}
