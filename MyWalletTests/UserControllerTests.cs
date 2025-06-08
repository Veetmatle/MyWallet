// UserControllerTests.cs
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyWallet.Controllers;
using MyWallet.DTOs;
using MyWallet.Mappers;
using MyWallet.Models;
using MyWallet.Services;
using NUnit.Framework;

namespace MyWalletTests
{
    [TestFixture]
    public class UserControllerTests
    {
        private Mock<IUserService> _userServiceMock = null!;
        private UserMapper _mapper = null!;
        private UserController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _userServiceMock = new Mock<IUserService>();
            _mapper = new UserMapper();
            // tutaj także NullLogger
            _controller = new UserController(
                _userServiceMock.Object,
                _mapper,
                NullLogger<UserController>.Instance
            );
        }

        [Test]
        public async Task Register_WithValidModel_Returns_OkResult()
        {
            var req = new RegisterRequest {
                Username = "testuser", Email = "t@t.pl", Password = "password123"
            };
            _userServiceMock
                .Setup(s => s.CreateUserAsync(It.IsAny<User>(), req.Password))
                .ReturnsAsync(true);

            var res = await _controller.Register(req);
            Assert.That(res, Is.InstanceOf<OkObjectResult>());

            var ok = res as OkObjectResult;
            Assert.That(ok!.Value, Is.EqualTo("Rejestracja zakończona sukcesem."));
        }

        [Test]
        public async Task GetById_WithValidId_Returns_User()
        {
            var user = new User { Id = 1, Username = "u", Email = "u@x.pl" };
            _userServiceMock
                .Setup(s => s.GetUserByIdAsync(1))
                .ReturnsAsync(user);

            var res = await _controller.GetById(1);
            Assert.That(res, Is.InstanceOf<OkObjectResult>());

            var ok = res as OkObjectResult;
            var dto = ok!.Value as UserDto;
            Assert.That(dto!.Username, Is.EqualTo("u"));
        }

        [Test]
        public void Logout_Clears_Session()
        {
            var sessionMock = new Mock<ISession>();
            var httpCtxMock = new Mock<HttpContext>();
            httpCtxMock.Setup(c => c.Session).Returns(sessionMock.Object);

            _controller.ControllerContext = new ControllerContext {
                HttpContext = httpCtxMock.Object
            };

            var res = _controller.Logout();
            Assert.That(res, Is.InstanceOf<OkObjectResult>());
            sessionMock.Verify(s => s.Clear(), Times.Once);
        }
    }
}
