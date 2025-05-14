// MyWallet/Services/Implementations/ExternalApiService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyWallet.Data;
using MyWallet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace MyWallet.Services.Implementations
{
    public class ExternalApiService : IExternalApiService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<ExternalApiService> _logger;

        public ExternalApiService(
            ApplicationDbContext context,
            IHttpClientFactory clientFactory,
            ILogger<ExternalApiService> logger)
        {
            _context = context;
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public async Task<decimal> GetCurrentPriceAsync(string symbol, string category)
        {
            try
            {
                var apiConfig = await GetApiConfigForCategoryAsync(category);
                if (apiConfig == null)
                {
                    _logger.LogError($"No API configuration found for category: {category}");
                    return 0;
                }

                var client = _clientFactory.CreateClient();
                var endpoint = "";

                // Określ odpowiedni endpoint API na podstawie kategorii
                switch (category.ToLower())
                {
                    case "cryptocurrency":
                        // Endpoint dla CoinGecko
                        endpoint = $"{apiConfig.BaseUrl}/simple/price?ids={symbol.ToLower()}&vs_currencies=usd";
                        var cryptoResponse = await client.GetFromJsonAsync<Dictionary<string, Dictionary<string, decimal>>>(endpoint);
                        return cryptoResponse[symbol.ToLower()]["usd"];

                    case "stock":
                        // Endpoint dla akcji (przykładowo)
                        endpoint = $"{apiConfig.BaseUrl}/quote?symbol={symbol}&apikey={apiConfig.ApiKey}";
                        var stockResponse = await client.GetFromJsonAsync<StockResponse>(endpoint);
                        return stockResponse.CurrentPrice;

                    // Dodaj więcej przypadków dla innych kategorii
                    default:
                        _logger.LogWarning($"Unsupported asset category: {category}");
                        return 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching price for {symbol} in category {category}");
                return 0;
            }
        }

        public async Task<Dictionary<string, decimal>> GetMultipleCurrentPricesAsync(IEnumerable<string> symbols, string category)
        {
            var result = new Dictionary<string, decimal>();

            try
            {
                var apiConfig = await GetApiConfigForCategoryAsync(category);
                if (apiConfig == null)
                {
                    _logger.LogError($"No API configuration found for category: {category}");
                    return result;
                }

                var client = _clientFactory.CreateClient();
                
                // Implementacja w zależności od kategorii aktywów
                switch (category.ToLower())
                {
                    case "cryptocurrency":
                        var symbolsList = string.Join(",", symbols.Select(s => s.ToLower()));
                        var endpoint = $"{apiConfig.BaseUrl}/simple/price?ids={symbolsList}&vs_currencies=usd";
                        var response = await client.GetFromJsonAsync<Dictionary<string, Dictionary<string, decimal>>>(endpoint);
                        
                        foreach (var symbol in symbols)
                        {
                            if (response.ContainsKey(symbol.ToLower()))
                            {
                                result[symbol] = response[symbol.ToLower()]["usd"];
                            }
                        }
                        break;

                    // Dodaj więcej przypadków dla innych kategorii
                    default:
                        // Dla kategorii bez masowego API, pobierz ceny pojedynczo
                        foreach (var symbol in symbols)
                        {
                            result[symbol] = await GetCurrentPriceAsync(symbol, category);
                        }
                        break;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching multiple prices for category {category}");
                return result;
            }
        }

        public async Task<Dictionary<DateTime, decimal>> GetHistoricalPricesAsync(string symbol, string category, DateTime startDate, DateTime endDate)
        {
            var result = new Dictionary<DateTime, decimal>();

            try
            {
                var apiConfig = await GetApiConfigForCategoryAsync(category);
                if (apiConfig == null)
                {
                    _logger.LogError($"No API configuration found for category: {category}");
                    return result;
                }

                var client = _clientFactory.CreateClient();
                
                // Implementacja w zależności od kategorii aktywów
                switch (category.ToLower())
                {
                    case "cryptocurrency":
                        // Konwersja dat do formatu Unix timestamp dla CoinGecko
                        var fromTimestamp = ((DateTimeOffset)startDate).ToUnixTimeSeconds();
                        var toTimestamp = ((DateTimeOffset)endDate).ToUnixTimeSeconds();
                        
                        var endpoint = $"{apiConfig.BaseUrl}/coins/{symbol.ToLower()}/market_chart/range?vs_currency=usd&from={fromTimestamp}&to={toTimestamp}";
                        var response = await client.GetFromJsonAsync<CryptoHistoricalResponse>(endpoint);
                        
                        foreach (var price in response.Prices)
                        {
                            // CoinGecko zwraca [timestamp, price]
                            var timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)price[0]).DateTime;
                            result[timestamp] = price[1];
                        }
                        break;

                    // Dodaj więcej przypadków dla innych kategorii
                    default:
                        _logger.LogWarning($"Historical data not implemented for category: {category}");
                        break;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching historical prices for {symbol} in category {category}");
                return result;
            }
        }

        private async Task<ExternalApiConfig> GetApiConfigForCategoryAsync(string category)
        {
            // Pobierz konfigurację API dla danej kategorii aktywów
            return await _context.ExternalApiConfigs
                .FirstOrDefaultAsync(c => c.Name.ToLower() == category.ToLower() && c.IsActive);
        }

        // Klasy pomocnicze do deserializacji odpowiedzi API
        private class StockResponse
        {
            public decimal CurrentPrice { get; set; }
        }

        private class CryptoHistoricalResponse
        {
            public List<List<decimal>> Prices { get; set; }
        }
    }
}