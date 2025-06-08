using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using MyWallet.Data;
using MyWallet.Models;
using MyWallet.Services;
using MyWallet.Services.Implementations;
using NUnit.Framework;

namespace MyWalletTests;

[TestFixture]
public class AssetServiceTests
{
    private static ApplicationDbContext Ctx(string db) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(db).Options);

    private AssetService Create(string dbName, out Mock<IExternalApiService> priceMock)
    {
        var ctx = Ctx(dbName);

        // musimy mieć Portfolio o ID = 1 (FK)
        ctx.Portfolios.Add(new Portfolio { Id = 1, Name = "P1", Description = "D", UserId = 1 });
        ctx.SaveChanges();

        priceMock = new Mock<IExternalApiService>();
        var portfolioSrv = new PortfolioService(ctx, priceMock.Object);

        return new AssetService(ctx, priceMock.Object, portfolioSrv);
    }

    [Test]
    public async Task CreateAssetAsync_Saves_New_Asset()
    {
        var svc = Create(nameof(CreateAssetAsync_Saves_New_Asset), out var price);
        price.Setup(p => p.GetCurrentPriceAsync("btc", "crypto")).ReturnsAsync(100);

        var saved = await svc.CreateAssetAsync(new Asset
        {
            Name = "Bitcoin",
            Symbol = "btc",
            Category = "crypto",
            PortfolioId = 1,
            Quantity = 2
        });

        Assert.That(saved.InvestedAmount, Is.EqualTo(200m));
    }

    [Test]
    public async Task SellAssetAsync_Reduces_Quantity()
    {
        var svc = Create(nameof(SellAssetAsync_Reduces_Quantity), out var price);
        price.Setup(p => p.GetCurrentPriceAsync("btc", "crypto")).ReturnsAsync(50);

        var a = await svc.CreateAssetAsync(new Asset
        {
            Name = "Bitcoin",
            Symbol = "btc",
            Category = "crypto",
            PortfolioId = 1,
            Quantity = 2
        });

        var after = await svc.SellAssetAsync(a.Id, 1, 75);

        Assert.That(after.Quantity, Is.EqualTo(1));
    }
}
