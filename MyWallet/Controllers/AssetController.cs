// AssetController.cs
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
        private readonly IExternalApiService _externalApi;
        private readonly IAssetService _assetService;
        private readonly AssetMapper _assetMapper;
        private readonly IExternalApiService  _external;

        public AssetController(
            IExternalApiService externalApi,
            IAssetService assetService,
            AssetMapper assetMapper,
            IExternalApiService external)
        {
            _externalApi  = externalApi;
            _assetService = assetService;
            _assetMapper  = assetMapper;
            _external     = external;  
        }

        // GET api/asset/search?category={category}&query={query}
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string category,
            [FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Query nie może być puste.");
            if (string.IsNullOrWhiteSpace(category))
                return BadRequest("Category nie może być pusta.");

            var hints = await _externalApi.SearchAssetsAsync(query, category);
            return Ok(hints);
        }

        // GET api/asset/price?category={category}&symbol={symbol}
        [HttpGet("price")]
        public async Task<IActionResult> GetPrice([FromQuery] string category, [FromQuery] string symbol)
        {
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(symbol))
                return BadRequest("category and symbol are required.");

            var price = await _external.GetCurrentPriceAsync(symbol, category);
            return Ok(price);
        }

        // GET api/asset/portfolio/{portfolioId}
        [HttpGet("portfolio/{portfolioId}")]
        public async Task<IActionResult> GetAssetsByPortfolio(int portfolioId)
        {
            var assets = await _assetService.GetPortfolioAssetsAsync(portfolioId);
            var dtoList = assets.Select(_assetMapper.ToDto);
            return Ok(dtoList);
        }

        // GET api/asset/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAssetById(int id)
        {
            var asset = await _assetService.GetAssetByIdAsync(id);
            if (asset == null)
                return NotFound();
            return Ok(_assetMapper.ToDto(asset));
        }

        // POST api/asset
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AssetDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var model   = _assetMapper.ToModel(dto);
            var created = await _assetService.CreateAssetAsync(model);
            return CreatedAtAction(
                nameof(GetAssetById),
                new { id = created.Id },
                _assetMapper.ToDto(created));
        }

        // PUT api/asset
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] AssetDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var model   = _assetMapper.ToModel(dto);
            var success = await _assetService.UpdateAssetAsync(model);
            if (!success)
                return NotFound();
            return Ok("Aktywo zostało zaktualizowane.");
        }

        // DELETE api/asset/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _assetService.DeleteAssetAsync(id);
            if (!success)
                return NotFound();
            return Ok("Aktywo zostało usunięte.");
        }

        // PUT api/asset/portfolio/{portfolioId}/update-prices
        [HttpPut("portfolio/{portfolioId}/update-prices")]
        public async Task<IActionResult> UpdateAssetPrices(int portfolioId)
        {
            await _assetService.UpdateAssetPricesAsync(portfolioId);
            return Ok("Ceny aktywów zaktualizowane.");
        }

        // GET api/asset/{id}/value
        [HttpGet("{id}/value")]
        public async Task<IActionResult> GetAssetCurrentValue(int id)
        {
            var value = await _assetService.CalculateAssetCurrentValueAsync(id);
            return Ok(value);
        }

        // GET api/asset/{id}/profitloss
        [HttpGet("{id}/profitloss")]
        public async Task<IActionResult> GetAssetProfitLoss(int id)
        {
            var profitLoss = await _assetService.CalculateAssetProfitLossAsync(id);
            return Ok(profitLoss);
        }

        // GET api/asset/{id}/history?start=yyyy-MM-dd&end=yyyy-MM-dd
        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetAssetPriceHistory(
            int id,
            [FromQuery] DateTime start,
            [FromQuery] DateTime end)
        {
            var history = await _assetService.GetAssetPriceHistoryAsync(id, start, end);
            return Ok(history);
        }

        // POST api/asset/{id}/upload-image
        [HttpPost("{id}/upload-image")]
        public async Task<IActionResult> UploadImage(int id, [FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Plik nie został przesłany.");

            var asset = await _assetService.GetAssetByIdAsync(id);
            if (asset == null)
                return NotFound("Aktywo nie istnieje.");

            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot/uploads/assets");
            Directory.CreateDirectory(uploadsFolder);

            var extension = Path.GetExtension(file.FileName);
            var fileName  = $"asset_{id}_{Guid.NewGuid()}{extension}";
            var filePath  = Path.Combine(uploadsFolder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            asset.ImagePath = $"uploads/assets/{fileName}";
            await _assetService.UpdateAssetAsync(asset);

            return Ok(new { imageUrl = asset.ImagePath });
        }
        
        [HttpGet("hints")]
        public async Task<IActionResult> GetHints([FromQuery] string category, [FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(query))
                return BadRequest("category and query are required.");

            var hints = await _external.SearchAssetsAsync(query, category);
            return Ok(hints);
        }
    }
}
