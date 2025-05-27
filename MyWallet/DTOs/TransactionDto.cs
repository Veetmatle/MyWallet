using System;
using System.ComponentModel.DataAnnotations;

namespace MyWallet.DTOs
{
    public class TransactionDto
    {
        public int Id { get; set; }
        
        [Range(1, int.MaxValue, ErrorMessage = "AssetId musi być dodatnie.")]
        public int? AssetId { get; set; }
        
        [Required(ErrorMessage = "Symbol aktywa jest wymagany.")]
        [StringLength(20, ErrorMessage = "Symbol może mieć maksymalnie 20 znaków.")]
        public string AssetSymbol { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335",
            ErrorMessage = "Cena musi być dodatnia.")]
        public decimal Price { get; set; }

        [Range(typeof(decimal), "0.00000001", "79228162514264337593543950335",
            ErrorMessage = "Ilość musi być większa od zera.")]
        public decimal Quantity { get; set; }
        
        [Required(ErrorMessage = "Typ transakcji jest wymagany.")]
        public string Type { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335",
            ErrorMessage = "Kwota całkowita musi być dodatnia.")]
        public decimal TotalAmount { get; set; }

        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "PortfolioId jest wymagane.")]
        [Range(1, int.MaxValue, ErrorMessage = "PortfolioId musi być większe od zera.")]
        public int PortfolioId { get; set; }
    }

}