using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MyWallet.Controllers;
using MyWallet.DTOs;
using MyWallet.Mappers;
using MyWallet.Models;
using MyWallet.Services;
using NUnit.Framework;

namespace MyWalletTests;

[TestFixture]
public class AssetControllerTests
{
    private Mock<IExternalApiService> _externalApiMock;
    private Mock<IAssetService> _assetServiceMock;
    private AssetMapper _mapper; // Prawdziwy mapper
    private AssetController _controller;

    [SetUp]
    public void Setup()
    {
        _externalApiMock = new Mock<IExternalApiService>();
        _assetServiceMock = new Mock<IAssetService>();
        _mapper = new AssetMapper(); // Prawdziwy mapper
        _controller = new AssetController(_externalApiMock.Object, _assetServiceMock.Object, _mapper, _externalApiMock.Object);
    }

    [Test]
    public async Task Search_WithValidQuery_Returns_OkResult()
    {
        // arrange
        var hints = new List<AssetHintDto>
        {
            new() { Symbol = "BTC", Name = "Bitcoin" },
            new() { Symbol = "ETH", Name = "Ethereum" }
        };

        _externalApiMock.Setup(s => s.SearchAssetsAsync("bit", "crypto")).ReturnsAsync(hints);

        // act
        var result = await _controller.Search("crypto", "bit");

        // assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult.Value, Is.EqualTo(hints));
    }

    [Test]
    public async Task GetAssetsByPortfolio_Returns_Assets()
    {
        // arrange
        var assets = new List<Asset>
        {
            new() { Id = 1, Symbol = "BTC", Name = "Bitcoin", CurrentPrice = 50000, Quantity = 1, PortfolioId = 1 },
            new() { Id = 2, Symbol = "ETH", Name = "Ethereum", CurrentPrice = 3000, Quantity = 2, PortfolioId = 1 }
        };

        _assetServiceMock.Setup(s => s.GetPortfolioAssetsAsync(1)).ReturnsAsync(assets);

        // act
        var result = await _controller.GetAssetsByPortfolio(1);

        // assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        var dtoList = okResult.Value as IEnumerable<AssetDto>;
        Assert.That(dtoList.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task Create_WithValidDto_Returns_CreatedAtAction()
    {
        // arrange
        var dto = new AssetDto { Symbol = "BTC", Name = "Bitcoin", CurrentPrice = 50000m, PortfolioId = 1 };
        var created = new Asset { Id = 1, Symbol = "BTC", Name = "Bitcoin", CurrentPrice = 50000m };

        _assetServiceMock.Setup(s => s.CreateAssetAsync(It.IsAny<Asset>(), 50000m)).ReturnsAsync(created);

        // act
        var result = await _controller.Create(dto);

        // assert
        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
    }

    [Test]
    public async Task Delete_WithValidId_Returns_OkResult()
    {
        // arrange
        _assetServiceMock.Setup(s => s.DeleteAssetAsync(1)).ReturnsAsync(true);

        // act
        var result = await _controller.Delete(1);

        // assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }
}
