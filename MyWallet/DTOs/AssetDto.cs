using System.ComponentModel.DataAnnotations;

namespace MyWallet.DTOs
{
    public class AssetDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Symbol aktywa jest wymagany.")]
        [StringLength(20, ErrorMessage = "Symbol może mieć maksymalnie 20 znaków.")]
        public string Symbol { get; set; }

        [Required(ErrorMessage = "Nazwa aktywa jest wymagana.")]
        [StringLength(100, ErrorMessage = "Nazwa może mieć maksymalnie 100 znaków.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Kategoria aktywa jest wymagana.")]
        [StringLength(50, ErrorMessage = "Kategoria może mieć maksymalnie 50 znaków.")]
        public string Category { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Cena musi być liczbą dodatnią.")]
        public decimal CurrentPrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Ilość musi być liczbą dodatnią.")]
        public decimal Quantity { get; set; }
        [Required(ErrorMessage = "ID portfela jest wymagane.")]
        public int PortfolioId { get; set; }
        
        public decimal AveragePurchasePrice { get; set; }

        public string? ImagePath { get; set; }
        public decimal  InvestedAmount       { get; set; }   
        
    }
}