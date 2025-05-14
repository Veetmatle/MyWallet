using MyWallet.DTOs;
using MyWallet.Models;
using Riok.Mapperly.Abstractions;

namespace MyWallet.Mappers
{
    [Mapper]
    public partial class TransactionMapper
    {
        [MapProperty(nameof(Transaction.Type), nameof(TransactionDto.Type))]
        public partial TransactionDto ToDto(Transaction transaction);
        
        [MapProperty(nameof(TransactionDto.Type), nameof(Transaction.Type))]
        public partial Transaction ToModel(TransactionDto dto);
        
        private string MapTransactionType(TransactionType type) => type.ToString();
        
        private TransactionType MapStringToTransactionType(string type)
        {
            if (Enum.TryParse<TransactionType>(type, out var result))
                return result;
            return TransactionType.Buy; // Domyślna wartość
        }
    }
}