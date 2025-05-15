namespace MyWallet.DTOs;
using System.ComponentModel.DataAnnotations;

public class PortfolioDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nazwa portfela jest wymagana")]
    [StringLength(100, ErrorMessage = "Nazwa może mieć maksymalnie 100 znaków")]
    public string Name { get; set; }

    [StringLength(500, ErrorMessage = "Opis może mieć maksymalnie 500 znaków")]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    [Required(ErrorMessage = "UserId jest wymagany")]
    public int UserId { get; set; }
}