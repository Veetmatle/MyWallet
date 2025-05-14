using MyWallet.DTOs;
using MyWallet.Models;
using Riok.Mapperly.Abstractions;

namespace MyWallet.Mappers
{
    [Mapper]
    public partial class AssetMapper
    {
        public partial AssetDto ToDto(Asset asset);
        public partial Asset ToModel(AssetDto dto);
    }
}