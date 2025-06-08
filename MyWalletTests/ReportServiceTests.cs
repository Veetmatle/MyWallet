using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyWallet.Models;
using MyWallet.Services;
using MyWallet.Services.Implementations;
using NUnit.Framework;

namespace MyWalletTests
{
    [TestFixture]
    public class ReportServiceTests
    {
        [Test]
        public async Task SendWeeklyReports_Sends_Email_For_Each_Portfolio()
        {
            // arrange
            var emailMock       = new Mock<IEmailService>();
            var portfolioMock   = new Mock<IPortfolioService>();
            var transactionMock = new Mock<ITransactionService>();

            var users = new List<User>
            {
                new() { Id = 1, Email = "a@a.pl" },
                new() { Id = 2, Email = "b@b.pl" }
            };
            portfolioMock.Setup(p => p.GetAllUsersAsync()).ReturnsAsync(users);
            portfolioMock.Setup(p => p.GetUserPortfoliosAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<Portfolio> { new() { Id = 10, Name = "P" } });

            transactionMock.Setup(t => t.GenerateReportPdfAsync(
                    It.IsAny<int>(), It.IsAny<System.DateTime>(), It.IsAny<System.DateTime>()))
                .ReturnsAsync(new byte[] { 1 });

            // pass NullLogger to satisfy new constructor
            var logger = NullLogger<ReportService>.Instance;
            var svc = new ReportService(
                emailMock.Object,
                portfolioMock.Object,
                transactionMock.Object,
                logger);

            // act
            await svc.SendWeeklyReports();

            // assert – 2 użytkowników × 1 portfel = 2 maili
            emailMock.Verify(e => e.SendEmailWithAttachmentAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<string>()),
                Times.Exactly(2));

            Assert.Pass(); // Verify rzuci, jeśli warunek niespełniony
        }
    }
}
