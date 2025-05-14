using Microsoft.AspNetCore.Mvc;
using MyWallet.DTOs;
using MyWallet.Mappers;
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
        private readonly UserMapper _userMapper;

        public UserController(IUserService userService, UserMapper userMapper)
        {
            _userService = userService;
            _userMapper = userMapper;
        }

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
                return Conflict("Użytkownik już istnieje.");

            return Ok("Rejestracja zakończona sukcesem.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            var isValid = await _userService.ValidateUserCredentialsAsync(model.UsernameOrEmail, model.Password);
            if (!isValid)
                return Unauthorized("Nieprawidłowe dane logowania.");

            var user = await _userService.GetUserByUsernameAsync(model.UsernameOrEmail)
                    ?? await _userService.GetUserByEmailAsync(model.UsernameOrEmail);

            return Ok(_userMapper.ToDto(user));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();

            return Ok(_userMapper.ToDto(user));
        }
    }

    public class RegisterRequest
    {
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class LoginRequest
    {
        public required string UsernameOrEmail { get; set; }
        public required string Password { get; set; }
    }
}
