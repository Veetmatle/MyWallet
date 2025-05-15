using System.ComponentModel.DataAnnotations;

namespace MyWallet.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nazwa użytkownika jest wymagana.")]
        [StringLength(50, ErrorMessage = "Login może mieć maksymalnie 50 znaków.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Adres e-mail jest wymagany.")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format adresu e-mail.")]
        [StringLength(100, ErrorMessage = "E-mail może mieć maksymalnie 100 znaków.")]
        public string Email { get; set; }
    }
}