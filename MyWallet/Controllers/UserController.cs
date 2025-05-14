using Microsoft.AspNetCore.Mvc;
using MyWallet.Models;
using MyWallet.Services;
using System.Threading.Tasks;

namespace MyWallet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // POST: api/user/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            var user = new User
            {
                Username = model.Username,
                Email = model.Email
            };

            var result = await _userService.CreateUserAsync(user, model.Password);
            if (!result)
            {
                return Conflict("Użytkownik o podanym loginie lub adresie email już istnieje.");
            }

            return Ok("Rejestracja zakończona sukcesem.");
        }

        // POST: api/user/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            var isValid = await _userService.ValidateUserCredentialsAsync(model.UsernameOrEmail, model.Password);
            if (!isValid)
            {
                return Unauthorized("Nieprawidłowe dane logowania.");
            }

            var user = await _userService.GetUserByUsernameAsync(model.UsernameOrEmail) 
                    ?? await _userService.GetUserByEmailAsync(model.UsernameOrEmail);

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email
                // Tu można później dodać JWT
            });
        }

        // GET: api/user/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound("Użytkownik nie istnieje.");
            }

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                user.CreatedAt
            });
        }
    }

    // Request DTOs

    public class RegisterRequest
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginRequest
    {
        public string UsernameOrEmail { get; set; }
        public string Password { get; set; }
    }
}
