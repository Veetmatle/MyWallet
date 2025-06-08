// UserController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<UserController> _logger;

        public UserController(
            IUserService userService,
            UserMapper userMapper,
            ILogger<UserController> logger)
        {
            _userService = userService;
            _userMapper = userMapper;
            _logger     = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            _logger.LogInformation("Register: próba rejestracji użytkownika {Username}.", model.Username);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Register: niepoprawne dane rejestracji.");
                return BadRequest(ModelState);
            }

            var user = new User
            {
                Username = model.Username,
                Email    = model.Email
            };

            var result = await _userService.CreateUserAsync(user, model.Password);
            if (!result)
            {
                _logger.LogWarning("Register: użytkownik {Username} już istnieje.", model.Username);
                return Conflict("Użytkownik już istnieje.");
            }

            _logger.LogInformation("Register: rejestracja użytkownika {Username} zakończona sukcesem.", model.Username);
            return Ok("Rejestracja zakończona sukcesem.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            _logger.LogInformation("Login: próba logowania (username/email)={User}.", model.UsernameOrEmail);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Login: niepoprawne dane logowania.");
                return BadRequest(ModelState);
            }

            var isValid = await _userService.ValidateUserCredentialsAsync(model.UsernameOrEmail, model.Password);
            if (!isValid)
            {
                _logger.LogWarning("Login: nieudane logowanie dla {User}.", model.UsernameOrEmail);
                return Unauthorized("Nieprawidłowe dane logowania.");
            }

            var user = await _userService.GetUserByUsernameAsync(model.UsernameOrEmail)
                       ?? await _userService.GetUserByEmailAsync(model.UsernameOrEmail);

            HttpContext.Session.SetInt32("UserId", user.Id);
            _logger.LogInformation("Login: użytkownik {UserId} zalogowany pomyślnie.", user.Id);

            return Ok(_userMapper.ToDto(user));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogInformation("GetById: pobieranie użytkownika {Id}.", id);

            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("GetById: użytkownik {Id} nie znaleziony.", id);
                return NotFound();
            }

            return Ok(_userMapper.ToDto(user));
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            _logger.LogInformation("GetCurrentUser: sesja UserId={UserId}.", userId);

            if (userId == null)
            {
                _logger.LogWarning("GetCurrentUser: brak zalogowanego użytkownika.");
                return Unauthorized("Użytkownik nie jest zalogowany.");
            }

            var user = await _userService.GetUserByIdAsync(userId.Value);
            if (user == null)
            {
                _logger.LogWarning("GetCurrentUser: użytkownik {UserId} nie znaleziony.", userId);
                return NotFound("Użytkownik nie został znaleziony.");
            }

            return Ok(new { 
                userId = user.Id,
                username = user.Username,
                email = user.Email,
                isAdmin = user.IsAdmin 
            });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            _logger.LogInformation("Logout: wylogowywanie użytkownika {UserId}.", userId);

            HttpContext.Session.Clear();
            return Ok("Wylogowano pomyślnie.");
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
