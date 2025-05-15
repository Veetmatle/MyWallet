using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace MyWallet.DTOs
{
    public class AssetImageUploadDto
    {
        [Required(ErrorMessage = "Id aktywa jest wymagane.")]
        [Range(1, int.MaxValue, ErrorMessage = "Id aktywa musi być większe od zera.")]
        public int AssetId { get; set; }

        [Required(ErrorMessage = "Plik obrazu jest wymagany.")]
        public IFormFile File { get; set; }
    }
}