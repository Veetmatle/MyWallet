using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
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
public class PortfolioControllerTests
{
    private Mock<IPortfolioService> _portfolioServiceMock;
    private PortfolioMapper _mapper;
    private Mock<IWebHostEnvironment> _envMock;
    private PortfolioController _controller;

    [SetUp]
    public void Setup()
    {
        _portfolioServiceMock = new Mock<IPortfolioService>();
        _mapper = new PortfolioMapper();
        _envMock = new Mock<IWebHostEnvironment>();
        _controller = new PortfolioController(_portfolioServiceMock.Object, _mapper, _envMock.Object);
    }

    [Test]
    public async Task GetPortfolioById_WithInvalidId_Returns_NotFound()
    {
        // arrange
        _portfolioServiceMock.Setup(s => s.GetPortfolioByIdAsync(999)).ReturnsAsync((Portfolio)null);

        // act
        var result = await _controller.GetPortfolioById(999);

        // assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task DeletePortfolio_WithValidId_Returns_OkResult()
    {
        // arrange
        _portfolioServiceMock.Setup(s => s.DeletePortfolioAsync(1)).ReturnsAsync(true);

        // act
        var result = await _controller.DeletePortfolio(1);

        // assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult.Value, Is.EqualTo("Portfel usunięty."));
    }

    [Test]
    public async Task GetPortfolioValue_Returns_Correct_Value()
    {
        // arrange
        _portfolioServiceMock.Setup(s => s.CalculatePortfolioValueAsync(1)).ReturnsAsync(50000m);

        // act
        var result = await _controller.GetPortfolioValue(1);

        // assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult.Value, Is.EqualTo(50000m));
    }

    [Test]
    public async Task GetProfitLoss_Returns_Correct_Value()
    {
        // arrange
        _portfolioServiceMock.Setup(s => s.GetPortfolioProfitLossAsync(1)).ReturnsAsync(5000m);

        // act
        var result = await _controller.GetProfitLoss(1);

        // assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult.Value, Is.EqualTo(5000m));
    }

    [Test]
    public async Task GetPortfolioChart_WithFutureEndDate_Returns_BadRequest()
    {
        // arrange
        var startDate = System.DateTime.Now.AddDays(-7);
        var endDate = System.DateTime.Now.AddDays(1); // future date

        // act
        var result = await _controller.GetPortfolioChart(1, startDate, endDate);

        // assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetPortfolioChart_WithNonExistentPortfolio_Returns_NotFound()
    {
        // arrange
        _portfolioServiceMock.Setup(s => s.PortfolioExistsAsync(999)).ReturnsAsync(false);

        // act
        var result = await _controller.GetPortfolioChart(999);

        // assert
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task UploadImage_WithNullFile_Returns_BadRequest()
    {
        // arrange
        _envMock.Setup(e => e.WebRootPath).Returns("/wwwroot");

        // act
        var result = await _controller.UploadImage(1, null);

        // assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequest = result as BadRequestObjectResult;
        Assert.That(badRequest.Value, Is.EqualTo("Nie wybrano pliku."));
    }

    [Test]
    public async Task UploadImage_WithNullWebRootPath_Returns_ServerError()
    {
        // arrange
        _envMock.Setup(e => e.WebRootPath).Returns((string)null);
        var fileMock = new Mock<IFormFile>();

        // act
        var result = await _controller.UploadImage(1, fileMock.Object);

        // assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = result as ObjectResult;
        Assert.That(objectResult.StatusCode, Is.EqualTo(500));
    }
}
