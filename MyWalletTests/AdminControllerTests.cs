using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWallet.Controllers;
using MyWallet.Data;
using MyWallet.Models;
using NUnit.Framework;
using System.Text;
using System.Threading;

namespace MyWalletTests;

[TestFixture]
public class AdminControllerTests
{
    // Własna implementacja ISession zamiast mockowania
    public class MockHttpSession : ISession
    {
        private readonly Dictionary<string, byte[]> _sessionStorage = new Dictionary<string, byte[]>();

        public string Id => "test-session";
        public bool IsAvailable => true;
        public IEnumerable<string> Keys => _sessionStorage.Keys;

        public void Clear() => _sessionStorage.Clear();

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key) => _sessionStorage.Remove(key);

        public void Set(string key, byte[] value) => _sessionStorage[key] = value;

        public bool TryGetValue(string key, out byte[] value) => _sessionStorage.TryGetValue(key, out value);
    }

    private static ApplicationDbContext Ctx(string db) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(db).Options);

    private AdminController SetupController(ApplicationDbContext db, int? userId = null)
    {
        var controller = new AdminController(db);
        var session = new MockHttpSession();
        
        // Ustaw userId w sesji jeśli podany
        if (userId.HasValue)
        {
            var userIdBytes = new byte[] 
            { 
                (byte)(userId.Value >> 24), 
                (byte)(0xFF & (userId.Value >> 16)), 
                (byte)(0xFF & (userId.Value >> 8)), 
                (byte)(0xFF & userId.Value) 
            };
            session.Set("UserId", userIdBytes);
        }
        
        var httpContext = new DefaultHttpContext { Session = session };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        
        return controller;
    }

    [Test]
    public async Task GetUsers_WithoutSession_Returns_Unauthorized()
    {
        // arrange
        var db = Ctx(nameof(GetUsers_WithoutSession_Returns_Unauthorized));
        var controller = SetupController(db, null); // Brak userId w sesji

        // act
        var result = await controller.GetUsers();

        // assert
        Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
    }

    [Test]
    public async Task GetUsers_WithNonAdminUser_Returns_Forbid()
    {
        // arrange
        var db = Ctx(nameof(GetUsers_WithNonAdminUser_Returns_Forbid));
        var controller = SetupController(db, 1);

        var user = new User 
        { 
            Username = "user", 
            Email = "user@test.com", 
            PasswordHash = "hashedpassword123",
            IsAdmin = false 
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // act
        var result = await controller.GetUsers();

        // assert
        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task GetUsers_WithAdminUser_Returns_UserList()
    {
        // arrange
        var db = Ctx(nameof(GetUsers_WithAdminUser_Returns_UserList));
        var controller = SetupController(db, 1);

        var admin = new User 
        { 
            Username = "admin", 
            Email = "admin@test.com", 
            PasswordHash = "hashedpassword123",
            IsAdmin = true 
        };
        var user = new User 
        { 
            Username = "user", 
            Email = "user@test.com", 
            PasswordHash = "hashedpassword456",
            IsAdmin = false 
        };
        db.Users.AddRange(admin, user);
        await db.SaveChangesAsync();

        // act
        var result = await controller.GetUsers();

        // assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        var users = okResult.Value as IEnumerable<object>;
        Assert.That(users.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task MakeAdmin_WithValidUser_Updates_IsAdmin()
    {
        // arrange
        var db = Ctx(nameof(MakeAdmin_WithValidUser_Updates_IsAdmin));
        var controller = SetupController(db, 1);

        var admin = new User 
        { 
            Username = "admin", 
            Email = "admin@test.com", 
            PasswordHash = "hashedpassword123",
            IsAdmin = true 
        };
        var user = new User 
        { 
            Username = "user", 
            Email = "user@test.com", 
            PasswordHash = "hashedpassword456",
            IsAdmin = false 
        };
        db.Users.AddRange(admin, user);
        await db.SaveChangesAsync();

        var userId = user.Id;

        // act
        var result = await controller.MakeAdmin(userId);

        // assert
        Assert.That(result, Is.InstanceOf<OkResult>());
        var updatedUser = await db.Users.FindAsync(userId);
        Assert.That(updatedUser.IsAdmin, Is.True);
    }

    [Test]
    public async Task RemoveAdmin_WithValidUser_Updates_IsAdmin()
    {
        // arrange
        var db = Ctx(nameof(RemoveAdmin_WithValidUser_Updates_IsAdmin));
        var controller = SetupController(db, 1);

        var admin = new User 
        { 
            Username = "admin", 
            Email = "admin@test.com", 
            PasswordHash = "hashedpassword123",
            IsAdmin = true 
        };
        var user = new User 
        { 
            Username = "user", 
            Email = "user@test.com", 
            PasswordHash = "hashedpassword456",
            IsAdmin = true 
        };
        db.Users.AddRange(admin, user);
        await db.SaveChangesAsync();

        var userId = user.Id;

        // act
        var result = await controller.RemoveAdmin(userId);

        // assert
        Assert.That(result, Is.InstanceOf<OkResult>());
        var updatedUser = await db.Users.FindAsync(userId);
        Assert.That(updatedUser.IsAdmin, Is.False);
    }
}
