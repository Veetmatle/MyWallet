// AdminControllerTests.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MyWallet.Controllers;
using MyWallet.Data;
using MyWallet.Models;
using NUnit.Framework;

namespace MyWalletTests
{
    [TestFixture]
    public class AdminControllerTests
    {
        // Własna implementacja ISession zamiast mockowania
        public class MockHttpSession : ISession
        {
            private readonly Dictionary<string, byte[]> _sessionStorage = new();

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

        private static ApplicationDbContext Ctx(string db)
            => new(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(db).Options);

        private AdminController SetupController(ApplicationDbContext db, int? userId = null)
        {
            // przekazujemy NullLogger, bo w testach nie potrzebujemy logów
            var controller = new AdminController(db, NullLogger<AdminController>.Instance);

            var session = new MockHttpSession();
            if (userId.HasValue)
            {
                // zapis int do bajtów big-endian
                var id = userId.Value;
                var bytes = new byte[]
                {
                    (byte)(id >> 24),
                    (byte)(0xFF & (id >> 16)),
                    (byte)(0xFF & (id >> 8)),
                    (byte)(0xFF & id)
                };
                session.Set("UserId", bytes);
            }

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { Session = session }
            };
            return controller;
        }

        [Test]
        public async Task GetUsers_WithoutSession_Returns_Unauthorized()
        {
            var db = Ctx(nameof(GetUsers_WithoutSession_Returns_Unauthorized));
            var ctrl = SetupController(db, null);

            var result = await ctrl.GetUsers();
            Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
        }

        [Test]
        public async Task GetUsers_WithNonAdminUser_Returns_Forbid()
        {
            var db = Ctx(nameof(GetUsers_WithNonAdminUser_Returns_Forbid));
            var ctrl = SetupController(db, 1);

            db.Users.Add(new User {
                Username = "user", Email = "u@x.pl", PasswordHash="h", IsAdmin = false
            });
            await db.SaveChangesAsync();

            var result = await ctrl.GetUsers();
            Assert.That(result, Is.InstanceOf<ForbidResult>());
        }

        [Test]
        public async Task GetUsers_WithAdminUser_Returns_UserList()
        {
            var db = Ctx(nameof(GetUsers_WithAdminUser_Returns_UserList));
            var ctrl = SetupController(db, 1);

            db.Users.AddRange(
                new User { Username="admin", Email="a@x.pl", PasswordHash="h", IsAdmin=true },
                new User { Username="user",  Email="u@x.pl", PasswordHash="h", IsAdmin=false }
            );
            await db.SaveChangesAsync();

            var result = await ctrl.GetUsers();
            Assert.That(result, Is.InstanceOf<OkObjectResult>());

            var ok = result as OkObjectResult;
            var list = (ok!.Value as IEnumerable<object>)!;
            Assert.That(list.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task MakeAdmin_WithValidUser_Updates_IsAdmin()
        {
            var db = Ctx(nameof(MakeAdmin_WithValidUser_Updates_IsAdmin));
            var ctrl = SetupController(db, 1);

            db.Users.AddRange(
                new User { Username="admin", Email="a@x.pl", PasswordHash="h", IsAdmin=true },
                new User { Username="user",  Email="u@x.pl", PasswordHash="h", IsAdmin=false }
            );
            await db.SaveChangesAsync();

            var userId = db.Users.Single(u => u.Username=="user").Id;
            var result = await ctrl.MakeAdmin(userId);

            Assert.That(result, Is.InstanceOf<OkResult>());
            var updated = await db.Users.FindAsync(userId);
            Assert.That(updated!.IsAdmin, Is.True);
        }

        [Test]
        public async Task RemoveAdmin_WithValidUser_Updates_IsAdmin()
        {
            var db = Ctx(nameof(RemoveAdmin_WithValidUser_Updates_IsAdmin));
            var ctrl = SetupController(db, 1);

            db.Users.AddRange(
                new User { Username="admin", Email="a@x.pl", PasswordHash="h", IsAdmin=true },
                new User { Username="user",  Email="u@x.pl", PasswordHash="h", IsAdmin=true }
            );
            await db.SaveChangesAsync();

            var userId = db.Users.Single(u => u.Username=="user").Id;
            var result = await ctrl.RemoveAdmin(userId);

            Assert.That(result, Is.InstanceOf<OkResult>());
            var updated = await db.Users.FindAsync(userId);
            Assert.That(updated!.IsAdmin, Is.False);
        }
    }
}
