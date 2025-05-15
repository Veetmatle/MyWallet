using Microsoft.AspNetCore.Mvc;
using MyWallet.DTOs;
using MyWallet.Models;
using MyWallet.Services;
using MyWallet.Mappers;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyWallet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssetController : ControllerBase
    {
        private readonly IAssetService _assetService;
        private readonly AssetMapper _assetMapper;

        public AssetController(IAssetService assetService, AssetMapper assetMapper)
        {
            _assetService = assetService;
            _assetMapper = assetMapper;
        }

        [HttpGet("portfolio/{portfolioId}")]
        public async Task<IActionResult> GetAssetsByPortfolio(int portfolioId)
        {
            var assets = await _assetService.GetPortfolioAssetsAsync(portfolioId);
            var dtoList = assets.Select(_assetMapper.ToDto);
            return Ok(dtoList);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAssetById(int id)
        {
            var asset = await _assetService.GetAssetByIdAsync(id);
            if (asset == null)
                return NotFound();

            var dto = _assetMapper.ToDto(asset);
            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AssetDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState); // ✅ WALIDACJA

            var model = _assetMapper.ToModel(dto);
            var created = await _assetService.CreateAssetAsync(model);
            return CreatedAtAction(nameof(GetAssetById), new { id = created.Id }, _assetMapper.ToDto(created));
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] AssetDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState); // ✅ WALIDACJA

            var model = _assetMapper.ToModel(dto);
            var success = await _assetService.UpdateAssetAsync(model);
            if (!success)
                return NotFound();

            return Ok("Aktywo zostało zaktualizowane.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _assetService.DeleteAssetAsync(id);
            if (!success)
                return NotFound();

            return Ok("Aktywo zostało usunięte.");
        }

        [HttpPut("portfolio/{portfolioId}/update-prices")]
        public async Task<IActionResult> UpdateAssetPrices(int portfolioId)
        {
            await _assetService.UpdateAssetPricesAsync(portfolioId);
            return Ok("Ceny aktywów zaktualizowane.");
        }

        [HttpGet("{id}/value")]
        public async Task<IActionResult> GetAssetCurrentValue(int id)
        {
            var value = await _assetService.CalculateAssetCurrentValueAsync(id);
            return Ok(value);
        }

        [HttpGet("{id}/profitloss")]
        public async Task<IActionResult> GetAssetProfitLoss(int id)
        {
            var profitLoss = await _assetService.CalculateAssetProfitLossAsync(id);
            return Ok(profitLoss);
        }

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

            asset.ImagePath = $"uploads/assets/{fileName}";
            await _assetService.UpdateAssetAsync(asset);

            return Ok(new { imageUrl = asset.ImagePath });
        }
    }
}
