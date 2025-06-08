using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWallet.Data;
using MyWallet.Models;

namespace MyWallet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null)
                return Unauthorized();

            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser == null || !currentUser.IsAdmin)
                return Forbid();

            var users = await _context.Users.Select(u => new {
                userId = u.Id, // Zmienione z u.UserId na u.Id
                username = u.Username,
                email = u.Email,
                isAdmin = u.IsAdmin
            }).ToListAsync();
            
            return Ok(users);
        }

        [HttpPost("make-admin/{userId}")]
        public async Task<IActionResult> MakeAdmin(int userId)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null)
                return Unauthorized();

            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser == null || !currentUser.IsAdmin)
                return Forbid();

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsAdmin = true;
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        [HttpPost("remove-admin/{userId}")]
        public async Task<IActionResult> RemoveAdmin(int userId)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null)
                return Unauthorized();

            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser == null || !currentUser.IsAdmin)
                return Forbid();

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsAdmin = false;
                await _context.SaveChangesAsync();
            }

            return Ok();
        }
    }
}
