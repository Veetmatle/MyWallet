using MyWallet.DTOs;
using MyWallet.Models;
using Riok.Mapperly.Abstractions;

namespace MyWallet.Mappers
{
    [Mapper]
    public partial class PortfolioMapper
    {
        public partial PortfolioDto ToDto(Portfolio portfolio);
        public partial Portfolio ToModel(PortfolioDto dto);
    }
}