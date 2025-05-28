using NUnit.Framework;
using Moq;
using MyWallet.Services.Implementations;
using MyWallet.Models;
using MyWallet.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using MyWallet.Services;

namespace MyWalletTests
{
    public class TransactionServiceTests
    {
        private ApplicationDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Test]
        public async Task SendReportPdfByEmailAsync_SendsEmail_WhenEmailExists()
        {
            // Arrange
            var emailServiceMock = new Mock<IEmailService>();
            var portfolioServiceMock = new Mock<IPortfolioService>();
            var userServiceMock = new Mock<IUserService>();
            var dbContext = CreateInMemoryDbContext();

            var portfolio = new Portfolio { Id = 1, Name = "Test Portfolio", Description = "Desc" };
            portfolioServiceMock.Setup(p => p.GetPortfolioByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(portfolio);
            userServiceMock.Setup(u => u.GetUserEmailByPortfolioIdAsync(It.IsAny<int>()))
                .ReturnsAsync("test@example.com");

            // Dodaj przykładową transakcję, aby raport nie był pusty
            dbContext.Transactions.Add(new Transaction
            {
                Id = 1,
                PortfolioId = 1,
                AssetId = 1,
                AssetSymbol = "btc",
                Price = 10000m,
                Quantity = 0.5m,
                TotalAmount = 5000m,
                Type = TransactionType.Buy,
                ExecutedAt = DateTime.UtcNow.AddDays(-3),
                Notes = "Test transaction"
            });
            await dbContext.SaveChangesAsync();

            var transactionService = new TransactionService(
                dbContext,
                emailServiceMock.Object,
                portfolioServiceMock.Object,
                userServiceMock.Object);

            // Act
            await transactionService.SendReportPdfByEmailAsync(1, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

            // Assert
            emailServiceMock.Verify(e => e.SendEmailWithAttachmentAsync(
                "test@example.com",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>()),
                Times.Once);
        }

        [Test]
        public void SendReportPdfByEmailAsync_ThrowsException_WhenEmailIsNull()
        {
            // Arrange
            var emailServiceMock = new Mock<IEmailService>();
            var portfolioServiceMock = new Mock<IPortfolioService>();
            var userServiceMock = new Mock<IUserService>();
            var dbContext = CreateInMemoryDbContext();

            portfolioServiceMock.Setup(p => p.GetPortfolioByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new Portfolio { Id = 1, Name = "Test Portfolio", Description = "Desc" });
            userServiceMock.Setup(u => u.GetUserEmailByPortfolioIdAsync(It.IsAny<int>()))
                .ReturnsAsync((string)null);

            var transactionService = new TransactionService(
                dbContext,
                emailServiceMock.Object,
                portfolioServiceMock.Object,
                userServiceMock.Object);

            // Act & Assert
            Assert.ThrowsAsync<Exception>(async () =>
                await transactionService.SendReportPdfByEmailAsync(1, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow));
        }
    }
}
