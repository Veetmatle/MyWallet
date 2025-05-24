using MyWallet.DTOs;
using MyWallet.Services;

namespace MyWallet.Services.Implementations
{
    public class OrderService : IOrderService
    {
        public Task<IEnumerable<OrderDto>> GetOpenOrdersAsync()
        {
            // Tymczasowy testowy zwrot – dodaj tu prawdziwą logikę później
            var orders = new List<OrderDto>
            {
                new() { Id = 1, Date = DateTime.Now, Description = "Zlecenie testowe 1" },
                new() { Id = 2, Date = DateTime.Now.AddDays(-1), Description = "Zlecenie testowe 2" }
            };

            return Task.FromResult<IEnumerable<OrderDto>>(orders);
        }
    }
}