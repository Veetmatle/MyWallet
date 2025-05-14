using Microsoft.AspNetCore.Http;

namespace MyWallet.DTOs
{
    public class AssetImageUploadDto
    {
        public int AssetId { get; set; }
        public IFormFile File { get; set; }
    }
}