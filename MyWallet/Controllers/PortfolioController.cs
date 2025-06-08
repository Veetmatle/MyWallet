using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyWallet.DTOs;
using MyWallet.Mappers;
using MyWallet.Models;
using MyWallet.Services;
using System;
using System.IO;
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
        private readonly IWebHostEnvironment _env;

        public PortfolioController(
            IPortfolioService portfolioService,
            PortfolioMapper portfolioMapper,
            IWebHostEnvironment env)
        {
            _portfolioService = portfolioService;
            _portfolioMapper = portfolioMapper;
            _env = env;
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

        [HttpGet("{id}/chart")]
        public async Task<IActionResult> GetPortfolioChart(
            int id,
            [FromQuery] DateTime? start = null,
            [FromQuery] DateTime? end = null)
        {
            try
            {
                var startDate = start ?? DateTime.Now.AddMonths(-12);
                var endDate = end ?? DateTime.Now;

                if (startDate > endDate)
                    return BadRequest(new { error = "Data początkowa nie może być późniejsza niż data końcowa." });

                if (endDate > DateTime.Now)
                    return BadRequest(new { error = "Data końcowa nie może być z przyszłości." });

                var portfolioExists = await _portfolioService.PortfolioExistsAsync(id);
                if (!portfolioExists)
                    return NotFound(new { error = "Portfolio nie zostało znalezione." });

                var imageBytes = await _portfolioService.GeneratePortfolioChartWithOxyPlotAsync(id, startDate, endDate);

                if (imageBytes == null || imageBytes.Length == 0)
                    return NotFound(new { error = "Brak danych do wygenerowania wykresu." });

                Response.Headers["Cache-Control"] = "public, max-age=300";
                Response.Headers["Content-Disposition"] = $"inline; filename=\"portfolio_{id}_chart.png\"";

                return File(imageBytes, "image/png");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Wystąpił błąd podczas generowania wykresu." });
            }
        }

        // ======================================
        // Nowa akcja do uploadu zdjęcia portfela
        // ======================================
        [HttpPost("{id}/upload-image")]
        public async Task<IActionResult> UploadImage(int id, IFormFile file)
        {
            // 1️⃣ Sprawdzenie, czy WebRootPath nie jest null/empty (czy app.UseStaticFiles() został wywołany)
            var webRootPath = _env.WebRootPath;
            if (string.IsNullOrEmpty(webRootPath))
            {
                return StatusCode(500, "WebRootPath jest pusty. Upewnij się, że w Program.cs wywołałeś `app.UseStaticFiles()` i masz fizyczny folder `wwwroot`.");
            }

            // 2️⃣ Walidacja pliku
            if (file == null || file.Length == 0)
                return BadRequest("Nie wybrano pliku.");

            if (!file.ContentType.StartsWith("image/"))
                return BadRequest("Dozwolone są tylko pliki graficzne.");

            // 3️⃣ Utworzenie katalogu wwwroot/images/portfolios (jeśli nie istnieje)
            string imagesFolder = Path.Combine(webRootPath, "images", "portfolios");
            if (!Directory.Exists(imagesFolder))
            {
                Directory.CreateDirectory(imagesFolder);
            }

            // 4️⃣ Generowanie unikalnej nazwy pliku (GUID + rozszerzenie)
            string extension = Path.GetExtension(file.FileName);
            string uniqueFileName = $"{Guid.NewGuid()}{extension}";

            // 5️⃣ Zapis pliku na dysku w katalogu imagesFolder
            string filePath = Path.Combine(imagesFolder, uniqueFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 6️⃣ Zbudowanie relatywnej ścieżki URL: "/images/portfolios/unikalnaNazwa.ext"
            string relativePath = Path.Combine("images", "portfolios", uniqueFileName)
                                      .Replace("\\", "/");
            string urlPath = "/" + relativePath;

            // 7️⃣ Pobranie encji Portfolio, aktualizacja ImagePath i zapis w bazie
            var portfolio = await _portfolioService.GetPortfolioByIdAsync(id);
            if (portfolio == null)
                return NotFound($"Nie znaleziono portfela o id {id}.");

            // (Opcjonalne) usunięcie starego pliku, jeśli był (odkomentuj, jeśli potrzebujesz):
            /*
            if (!string.IsNullOrEmpty(portfolio.ImagePath))
            {
                var existingFile = Path.Combine(webRootPath, portfolio.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(existingFile))
                {
                    System.IO.File.Delete(existingFile);
                }
            }
            */

            portfolio.ImagePath = urlPath;
            var success = await _portfolioService.UpdatePortfolioAsync(portfolio);
            if (!success)
                return StatusCode(StatusCodes.Status500InternalServerError, "Nie udało się zaktualizować portfela.");

            var dto = _portfolioMapper.ToDto(portfolio);
            return Ok(dto);
        }
    }
}
