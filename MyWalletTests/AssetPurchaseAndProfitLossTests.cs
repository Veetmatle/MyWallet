using Microsoft.EntityFrameworkCore;
using MyWallet.Data;
using MyWallet.Models;
using MyWallet.Services;
using MyWallet.Services.Implementations;
using MyWallet.Controllers;
using MyWallet.Mappers;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using MyWallet.DTOs;

namespace MyWallet.Tests
{
    [TestFixture]
    public class AssetPurchaseAndProfitLossTests
    {
        private ApplicationDbContext _context;
        private AssetService _assetService;
        private TransactionService _transactionService;
        private TransactionController _transactionController;
        private Mock<IExternalApiService> _mockExternalApi;
        private User _testUser;
        private Portfolio _testPortfolio;

        [SetUp]
        public async Task SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            _mockExternalApi = new Mock<IExternalApiService>();

            _assetService = new AssetService(_context, _mockExternalApi.Object);
            _transactionService = new TransactionService(_context, _assetService);

            var mapper = new TransactionMapper();

            _transactionController = new TransactionController(
                _transactionService,
                mapper,
                _assetService
            );

            await SetupTestDataAsync();
        }

        [TearDown]
        public void TearDown() => _context?.Dispose();

        private async Task SetupTestDataAsync()
        {
            _testUser = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hash"
            };

            _context.Users.Add(_testUser);
            await _context.SaveChangesAsync();

            _testPortfolio = new Portfolio
            {
                Id = 1,
                Name = "Test Portfolio",
                Description = "Test portfolio for unit testing",
                UserId = _testUser.Id,
                User = _testUser
            };

            _context.Portfolios.Add(_testPortfolio);
            await _context.SaveChangesAsync();
        }

        [Test]
        public async Task CreateAsset_FirstPurchase_ShouldSetCorrectAveragePrice()
        {
            var initialPrice = 100m;
            var quantity = 10m;

            _mockExternalApi.Setup(x => x.GetCurrentPriceAsync("btc", "cryptocurrency"))
                            .ReturnsAsync(initialPrice);

            var asset = new Asset
            {
                Symbol = "btc",
                Name = "Bitcoin",
                Category = "cryptocurrency",
                Quantity = quantity,
                PortfolioId = _testPortfolio.Id
            };

            var result = await _assetService.CreateAssetAsync(asset);

            Assert.That(result.AveragePurchasePrice, Is.EqualTo(initialPrice));
            Assert.That(result.InvestedAmount, Is.EqualTo(initialPrice * quantity));
            Assert.That(result.Quantity, Is.EqualTo(quantity));
        }

        [Test]
        public async Task CreateAsset_SecondPurchase_ShouldCalculateCorrectAveragePrice()
        {
            var firstPrice = 100m;
            var firstQty = 10m;
            var secondPrice = 150m;
            var secondQty = 5m;

            _mockExternalApi.Setup(x => x.GetCurrentPriceAsync("btc", "cryptocurrency"))
                            .ReturnsAsync(firstPrice);

            await _assetService.CreateAssetAsync(new Asset
            {
                Symbol = "btc",
                Name = "Bitcoin",
                Category = "cryptocurrency",
                Quantity = firstQty,
                PortfolioId = _testPortfolio.Id
            });

            _mockExternalApi.Setup(x => x.GetCurrentPriceAsync("btc", "cryptocurrency"))
                            .ReturnsAsync(secondPrice);

            var result = await _assetService.CreateAssetAsync(new Asset
            {
                Symbol = "btc",
                Name = "Bitcoin",
                Category = "cryptocurrency",
                Quantity = secondQty,
                PortfolioId = _testPortfolio.Id
            });

            var expectedCost = firstPrice * firstQty + secondPrice * secondQty;
            var expectedQty = firstQty + secondQty;
            var expectedAvg = expectedCost / expectedQty;

            Assert.That(result.Quantity, Is.EqualTo(expectedQty));
            Assert.That(result.InvestedAmount, Is.EqualTo(expectedCost));
            Assert.That(result.AveragePurchasePrice, Is.EqualTo(expectedAvg));

            var count = await _context.Assets.CountAsync(a => a.Symbol == "btc" && a.PortfolioId == _testPortfolio.Id);
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public async Task CreateAsset_MultiplePurchases_ShouldMaintainCorrectAverage()
        {
            var purchases = new[]
            {
                new { Price = 100m, Quantity = 10m },
                new { Price = 120m, Quantity = 5m  },
                new { Price =  80m, Quantity = 15m },
                new { Price = 110m, Quantity = 8m  }
            };

            decimal totalCost = 0;
            decimal totalQty = 0;

            foreach (var p in purchases)
            {
                _mockExternalApi.Setup(x => x.GetCurrentPriceAsync("eth", "cryptocurrency"))
                                .ReturnsAsync(p.Price);

                await _assetService.CreateAssetAsync(new Asset
                {
                    Symbol = "eth",
                    Name = "Ethereum",
                    Category = "cryptocurrency",
                    Quantity = p.Quantity,
                    PortfolioId = _testPortfolio.Id
                });

                totalCost += p.Price * p.Quantity;
                totalQty += p.Quantity;
            }

            var asset = await _context.Assets.FirstAsync(a => a.Symbol == "eth");
            var expectedAvg = totalCost / totalQty;

            Assert.That(asset.Quantity, Is.EqualTo(totalQty));
            Assert.That(asset.InvestedAmount, Is.EqualTo(totalCost));
            Assert.That(asset.AveragePurchasePrice, Is.EqualTo(expectedAvg).Within(0.00000001m));
        }

        [Test]
        public async Task GetPortfolioProfitLoss_WithProfitableAssets_ShouldReturnPositiveResult()
        {
            var purchasePrice = 100m;
            var currentPrice = 150m;
            var quantity = 10m;

            _mockExternalApi.Setup(x => x.GetCurrentPriceAsync("ada", "cryptocurrency"))
                            .ReturnsAsync(purchasePrice);

            await _assetService.CreateAssetAsync(new Asset
            {
                Symbol = "ada",
                Name = "Cardano",
                Category = "cryptocurrency",
                Quantity = quantity,
                PortfolioId = _testPortfolio.Id
            });

            var ada = await _context.Assets.FirstAsync(a => a.Symbol == "ada");
            ada.CurrentPrice = currentPrice;
            await _context.SaveChangesAsync();

            await _transactionService.CreateTransactionAsync(new Transaction
            {
                Type = TransactionType.Deposit,
                PortfolioId = _testPortfolio.Id,
                TotalAmount = purchasePrice * quantity,
                ExecutedAt = DateTime.UtcNow,
                AssetSymbol = "ada",
                Notes = "Test deposit for ADA"
            });

            var action = await _transactionController.GetPortfolioProfitLoss(_testPortfolio.Id);
            var ok = action as Microsoft.AspNetCore.Mvc.OkObjectResult;

            var dto = ok.Value as PortfolioProfitLossDto;

            var expectedProfit = (currentPrice - purchasePrice) * quantity;
            var expectedPerc = Math.Round(expectedProfit / (purchasePrice * quantity) * 100, 2);

            Assert.That(dto.IsProfit, Is.True);
            Assert.That(dto.ProfitLoss, Is.EqualTo(expectedProfit));
            Assert.That(dto.ProfitLossPercentage, Is.EqualTo(expectedPerc));
        }

        [Test]
        public async Task GetPortfolioProfitLoss_WithLossAssets_ShouldReturnNegativeResult()
        {
            var purchasePrice = 200m;
            var currentPrice = 120m;
            var quantity = 8m;

            _mockExternalApi.Setup(x => x.GetCurrentPriceAsync("dot", "cryptocurrency"))
                            .ReturnsAsync(purchasePrice);

            await _assetService.CreateAssetAsync(new Asset
            {
                Symbol = "dot",
                Name = "Polkadot",
                Category = "cryptocurrency",
                Quantity = quantity,
                PortfolioId = _testPortfolio.Id
            });

            var dot = await _context.Assets.FirstAsync(a => a.Symbol == "dot");
            dot.CurrentPrice = currentPrice;
            await _context.SaveChangesAsync();

            await _transactionService.CreateTransactionAsync(new Transaction
            {
                Type = TransactionType.Deposit,
                PortfolioId = _testPortfolio.Id,
                TotalAmount = purchasePrice * quantity,
                ExecutedAt = DateTime.UtcNow,
                AssetSymbol = "dot",
                Notes = "Test deposit for DOT"
            });

            var action = await _transactionController.GetPortfolioProfitLoss(_testPortfolio.Id);
            var ok = action as Microsoft.AspNetCore.Mvc.OkObjectResult;

            var dto = ok.Value as PortfolioProfitLossDto;

            var expectedLoss = (currentPrice - purchasePrice) * quantity;
            var expectedPerc = Math.Round(expectedLoss / (purchasePrice * quantity) * 100, 2);

            Assert.That(dto.IsProfit, Is.False);
            Assert.That(dto.ProfitLoss, Is.EqualTo(expectedLoss));
            Assert.That(dto.ProfitLossPercentage, Is.EqualTo(expectedPerc));
        }

        [Test]
        public async Task GetPortfolioProfitLossBreakdown_WithMixedAssets_ShouldReturnDetailedBreakdown()
        {
            _mockExternalApi.Setup(x => x.GetCurrentPriceAsync("btc", "cryptocurrency"))
                            .ReturnsAsync(50000m);

            await _assetService.CreateAssetAsync(new Asset
            {
                Symbol = "btc",
                Name = "Bitcoin",
                Category = "cryptocurrency",
                Quantity = 1m,
                PortfolioId = _testPortfolio.Id
            });

            var btc = await _context.Assets.FirstAsync(a => a.Symbol == "btc");
            btc.CurrentPrice = 60000m;

            _mockExternalApi.Setup(x => x.GetCurrentPriceAsync("eth", "cryptocurrency"))
                            .ReturnsAsync(3000m);

            await _assetService.CreateAssetAsync(new Asset
            {
                Symbol = "eth",
                Name = "Ethereum",
                Category = "cryptocurrency",
                Quantity = 5m,
                PortfolioId = _testPortfolio.Id
            });

            var eth = await _context.Assets.FirstAsync(a => a.Symbol == "eth");
            eth.CurrentPrice = 2500m;
            await _context.SaveChangesAsync();

            var action = await _transactionController.GetPortfolioProfitLossBreakdown(_testPortfolio.Id);
            var ok = action as Microsoft.AspNetCore.Mvc.OkObjectResult;

            var dto = ok.Value as PortfolioProfitLossBreakdownDto;

            Assert.That(dto.Assets.Count(), Is.EqualTo(2));
            Assert.That(dto.Summary.TotalAssets, Is.EqualTo(2));
            Assert.That(dto.Summary.ProfitableAssets, Is.EqualTo(1));
            
            var btcAsset = dto.Assets.First(a => a.Symbol == "btc");
            Assert.That(btcAsset.IsProfit, Is.True);
            Assert.That(btcAsset.ProfitLoss, Is.EqualTo(10000m)); // 60000 - 50000
    
            var ethAsset = dto.Assets.First(a => a.Symbol == "eth");
            Assert.That(ethAsset.IsProfit, Is.False);
            Assert.That(ethAsset.ProfitLoss, Is.EqualTo(-2500m)); // (2500 - 3000) * 5
        }

        [Test]
        public async Task CreateAsset_WithZeroQuantity_ShouldSucceedAndZeroInvested()
        {
            _mockExternalApi.Setup(x => x.GetCurrentPriceAsync("invalid", "cryptocurrency"))
                            .ReturnsAsync(100m);

            var result = await _assetService.CreateAssetAsync(new Asset
            {
                Symbol = "invalid",
                Name = "Invalid Asset",
                Category = "cryptocurrency",
                Quantity = 0m,
                PortfolioId = _testPortfolio.Id
            });

            Assert.That(result.Quantity, Is.EqualTo(0m));
            Assert.That(result.InvestedAmount, Is.EqualTo(0m));
        }

        [Test]
        public async Task CalculateAssetProfitLoss_AfterMultiplePurchases_ShouldBeAccurate()
        {
            var purchases = new[]
            {
                new { Price = 1000m, Quantity = 2m },
                new { Price = 1500m, Quantity = 1m },
                new { Price =  800m, Quantity = 3m }
            };

            foreach (var p in purchases)
            {
                _mockExternalApi.Setup(x => x.GetCurrentPriceAsync("link", "cryptocurrency"))
                                .ReturnsAsync(p.Price);

                await _assetService.CreateAssetAsync(new Asset
                {
                    Symbol = "link",
                    Name = "Chainlink",
                    Category = "cryptocurrency",
                    Quantity = p.Quantity,
                    PortfolioId = _testPortfolio.Id
                });
            }

            var link = await _context.Assets.FirstAsync(a => a.Symbol == "link");
            link.CurrentPrice = 1200m;
            await _context.SaveChangesAsync();

            var profit = await _assetService.CalculateAssetProfitLossAsync(link.Id);

            var totalInvested = 1000m * 2 + 1500m * 1 + 800m * 3;
            var totalQty = 6m;
            var currentVal = 1200m * totalQty;
            var expected = currentVal - totalInvested;

            Assert.That(profit, Is.EqualTo(expected));
            Assert.That(link.AveragePurchasePrice, Is.EqualTo(totalInvested / totalQty).Within(0.00000001m));
        }
    }
}