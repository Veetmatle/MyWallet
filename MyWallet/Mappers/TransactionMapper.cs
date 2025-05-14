using MyWallet.DTOs;
using MyWallet.Models;
using Riok.Mapperly.Abstractions;

namespace MyWallet.Mappers
{
    [Mapper]
    public partial class TransactionMapper
    {
        public partial TransactionDto ToDto(Transaction transaction);
        public partial Transaction ToModel(TransactionDto dto);
    }
}