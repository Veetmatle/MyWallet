using NUnit.Framework;
using MyWallet.Services.Implementations;
using MyWallet.Data;
using MyWallet.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace MyWalletTests
{
    public class UserServiceTests
    {
        private ApplicationDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(System.Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Test]
        public async Task GetUserEmailByPortfolioIdAsync_ReturnsEmail_WhenUserExists()
        {
            var dbContext = CreateInMemoryDbContext();
            var user = new User
            {
                Id = 1,
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = "dummyhash"
            };
            var portfolio = new Portfolio
            {
                Id = 1,
                UserId = 1,
                Name = "Test Portfolio",
                Description = "Test Description"
            };
            dbContext.Users.Add(user);
            dbContext.Portfolios.Add(portfolio);
            dbContext.SaveChanges();

            var userService = new UserService(dbContext);

            var email = await userService.GetUserEmailByPortfolioIdAsync(1);

            Assert.That(email, Is.EqualTo("test@example.com"));
        }


        [Test]
        public async Task GetUserEmailByPortfolioIdAsync_ReturnsNull_WhenNoUserFound()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var userService = new UserService(dbContext);

            // Act
            var email = await userService.GetUserEmailByPortfolioIdAsync(999); // nieistniejący portfolioId

            // Assert
            Assert.That(email, Is.Null);
        }
    }
}