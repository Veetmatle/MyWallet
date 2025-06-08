using System;
using System.Linq;
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
public class PortfolioServiceTests
{
    private static ApplicationDbContext Ctx(string db) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(db).Options);

    [Test]
    public async Task CreatePortfolioAsync_Saves_Portfolio()
    {
        var db      = Ctx(nameof(CreatePortfolioAsync_Saves_Portfolio));
        var service = new PortfolioService(db, new Mock<IExternalApiService>().Object);

        var p = new Portfolio { Name = "Test", Description = "Desc", UserId = 1 };

        var created = await service.CreatePortfolioAsync(p);

        Assert.That(await db.Portfolios.CountAsync(), Is.EqualTo(1));
        Assert.That(created.Description, Is.EqualTo("Desc"));
    }

    [Test]
    public async Task CalculatePortfolioValueAsync_Returns_Correct_Sum()
    {
        var db  = Ctx(nameof(CalculatePortfolioValueAsync_Returns_Correct_Sum));
        var srv = new PortfolioService(db, new Mock<IExternalApiService>().Object);

        var port = new Portfolio { Name = "Val", Description = "D", UserId = 1 };
        port.Assets.Add(new Asset { Name="BTC", Symbol = "btc", Category = "crypto", Quantity = 2, CurrentPrice = 10 });
        port.Assets.Add(new Asset { Name="ETH", Symbol = "eth", Category = "crypto", Quantity = 1, CurrentPrice = 5 });

        db.Portfolios.Add(port);
        await db.SaveChangesAsync();

        var sum = await srv.CalculatePortfolioValueAsync(port.Id);

        Assert.That(sum, Is.EqualTo(25));
    }

    [Test]
    public async Task AssetCategoryDistributionAsync_Computes_Percentages()
    {
        var db  = Ctx(nameof(AssetCategoryDistributionAsync_Computes_Percentages));
        var srv = new PortfolioService(db, new Mock<IExternalApiService>().Object);

        var p = new Portfolio { Name = "Dist", Description = "D", UserId = 1 };
        p.Assets.Add(new Asset { Name="BTC", Symbol = "btc", Category = "crypto", Quantity = 1, CurrentPrice = 30 });
        p.Assets.Add(new Asset { Name="SPY", Symbol = "spy", Category = "stock",  Quantity = 1, CurrentPrice = 70 });

        db.Portfolios.Add(p);
        await db.SaveChangesAsync();

        var dist = await srv.GetAssetCategoryDistributionAsync(p.Id);

        Assert.That(dist["crypto"], Is.EqualTo(30m));
        Assert.That(dist["stock"],  Is.EqualTo(70m));
    }

    [Test]
    public async Task RecordPortfolioHistoryAsync_Adds_Row()
    {
        var db  = Ctx(nameof(RecordPortfolioHistoryAsync_Adds_Row));
        var srv = new PortfolioService(db, new Mock<IExternalApiService>().Object);

        var p = new Portfolio { Name = "Hist", Description = "D", UserId = 1 };
        db.Portfolios.Add(p);
        await db.SaveChangesAsync();

        await srv.RecordPortfolioHistoryAsync(p.Id);

        Assert.That(db.PortfolioHistories.Count(), Is.EqualTo(1));
    }
}
