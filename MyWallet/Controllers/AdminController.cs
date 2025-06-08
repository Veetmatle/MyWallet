// AdminController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyWallet.Data;
using MyWallet.Models;

namespace MyWallet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            _logger.LogInformation("GetUsers: rozpoczęcie przetwarzania żądania.");

            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null)
            {
                _logger.LogWarning("GetUsers: brak sesji użytkownika. Unauthorized.");
                return Unauthorized();
            }

            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser == null || !currentUser.IsAdmin)
            {
                _logger.LogWarning("GetUsers: użytkownik {UserId} nie ma uprawnień admina.", currentUserId);
                return Forbid();
            }

            var users = await _context.Users
                .Select(u => new {
                    userId = u.Id,
                    username = u.Username,
                    email = u.Email,
                    isAdmin = u.IsAdmin
                })
                .ToListAsync();

            _logger.LogInformation("GetUsers: zwrócono {Count} użytkowników.", users.Count);
            return Ok(users);
        }

        [HttpPost("make-admin/{userId}")]
        public async Task<IActionResult> MakeAdmin(int userId)
        {
            _logger.LogInformation("MakeAdmin: próba nadania roli admin dla użytkownika {TargetId}.", userId);

            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null)
            {
                _logger.LogWarning("MakeAdmin: brak sesji użytkownika. Unauthorized.");
                return Unauthorized();
            }

            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser == null || !currentUser.IsAdmin)
            {
                _logger.LogWarning("MakeAdmin: użytkownik {UserId} nie ma uprawnień admina.", currentUserId);
                return Forbid();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsAdmin = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation("MakeAdmin: użytkownik {TargetId} został oznaczony jako admin.", userId);
            }
            else
            {
                _logger.LogWarning("MakeAdmin: użytkownik {TargetId} nie istnieje.", userId);
            }

            return Ok();
        }

        [HttpPost("remove-admin/{userId}")]
        public async Task<IActionResult> RemoveAdmin(int userId)
        {
            _logger.LogInformation("RemoveAdmin: próba odebrania roli admin dla użytkownika {TargetId}.", userId);

            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null)
            {
                _logger.LogWarning("RemoveAdmin: brak sesji użytkownika. Unauthorized.");
                return Unauthorized();
            }

            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser == null || !currentUser.IsAdmin)
            {
                _logger.LogWarning("RemoveAdmin: użytkownik {UserId} nie ma uprawnień admina.", currentUserId);
                return Forbid();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsAdmin = false;
                await _context.SaveChangesAsync();
                _logger.LogInformation("RemoveAdmin: użytkownik {TargetId} stracił rolę admina.", userId);
            }
            else
            {
                _logger.LogWarning("RemoveAdmin: użytkownik {TargetId} nie istnieje.", userId);
            }

            return Ok();
        }
    }
}
