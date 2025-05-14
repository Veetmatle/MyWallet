using MyWallet.DTOs;
using MyWallet.Models;
using Riok.Mapperly.Abstractions;

namespace MyWallet.Mappers
{
    [Mapper]
    public partial class UserMapper
    {
        public partial UserDto ToDto(User user);
        public partial User ToModel(UserDto dto);
    }
}