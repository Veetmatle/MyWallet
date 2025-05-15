using Microsoft.AspNetCore.Mvc;
using MyWallet.DTOs;
using MyWallet.Mappers;
using MyWallet.Models;
using MyWallet.Services;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

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
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
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
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
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
        [Required(ErrorMessage = "Login jest wymagany.")]
        [StringLength(50, ErrorMessage = "Login może mieć maksymalnie 50 znaków.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "E-mail jest wymagany.")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format adresu e-mail.")]
        [StringLength(100, ErrorMessage = "E-mail może mieć maksymalnie 100 znaków.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Hasło jest wymagane.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Hasło musi mieć co najmniej 6 znaków.")]
        public string Password { get; set; }
    }

    public class LoginRequest
    {
        [Required(ErrorMessage = "Login lub e-mail jest wymagany.")]
        public string UsernameOrEmail { get; set; }

        [Required(ErrorMessage = "Hasło jest wymagane.")]
        public string Password { get; set; }
    }
}
