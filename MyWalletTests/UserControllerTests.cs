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
public class UserControllerTests
{
    private Mock<IUserService> _userServiceMock;
    private UserMapper _mapper;
    private UserController _controller;

    [SetUp]
    public void Setup()
    {
        _userServiceMock = new Mock<IUserService>();
        _mapper = new UserMapper();
        _controller = new UserController(_userServiceMock.Object, _mapper);
    }

    [Test]
    public async Task Register_WithValidModel_Returns_OkResult()
    {
        // arrange
        var request = new RegisterRequest 
        { 
            Username = "testuser", 
            Email = "test@test.com", 
            Password = "password123" 
        };

        _userServiceMock.Setup(s => s.CreateUserAsync(It.IsAny<User>(), "password123")).ReturnsAsync(true);

        // act
        var result = await _controller.Register(request);

        // assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult.Value, Is.EqualTo("Rejestracja zakończona sukcesem."));
    }

    [Test]
    public async Task GetById_WithValidId_Returns_User()
    {
        // arrange
        var user = new User { Id = 1, Username = "testuser", Email = "test@test.com" };
        _userServiceMock.Setup(s => s.GetUserByIdAsync(1)).ReturnsAsync(user);

        // act
        var result = await _controller.GetById(1);

        // assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        var dto = okResult.Value as UserDto;
        Assert.That(dto.Username, Is.EqualTo("testuser"));
    }

    [Test]
    public void Logout_Clears_Session()
    {
        // arrange
        var sessionMock = new Mock<ISession>();
        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(c => c.Session).Returns(sessionMock.Object);
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };

        sessionMock.Setup(s => s.Clear());

        // act
        var result = _controller.Logout();

        // assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        sessionMock.Verify(s => s.Clear(), Times.Once);
    }
}
