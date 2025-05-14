using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyWallet.Data;
using MyWallet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
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
                        if (cryptoResponse != null && 
                            cryptoResponse.ContainsKey(symbol.ToLower()) && 
                            cryptoResponse[symbol.ToLower()].ContainsKey("usd"))
                        {
                            return cryptoResponse[symbol.ToLower()]["usd"];
                        }
                        return 0;

                    case "stock":
                        // Endpoint dla Alpha Vantage API (akcje)
                        endpoint = $"{apiConfig.BaseUrl}/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={apiConfig.ApiKey}";
                        var response = await client.GetAsync(endpoint);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            var contentStream = await response.Content.ReadAsStreamAsync();
                            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            var stockQuote = await JsonSerializer.DeserializeAsync<AlphaVantageQuote>(contentStream, options);
                            
                            if (stockQuote?.GlobalQuote?.Price > 0)
                            {
                                return stockQuote.GlobalQuote.Price;
                            }
                        }
                        return 0;

                    case "etf":
                        // ETF też możemy pobierać przez Alpha Vantage
                        endpoint = $"{apiConfig.BaseUrl}/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={apiConfig.ApiKey}";
                        var etfResponse = await client.GetAsync(endpoint);
                        
                        if (etfResponse.IsSuccessStatusCode)
                        {
                            var contentStream = await etfResponse.Content.ReadAsStreamAsync();
                            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            var etfQuote = await JsonSerializer.DeserializeAsync<AlphaVantageQuote>(contentStream, options);
                            
                            if (etfQuote?.GlobalQuote?.Price > 0)
                            {
                                return etfQuote.GlobalQuote.Price;
                            }
                        }
                        return 0;

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
                        
                        if (response != null)
                        {
                            foreach (var symbol in symbols)
                            {
                                if (response.ContainsKey(symbol.ToLower()) && 
                                    response[symbol.ToLower()].ContainsKey("usd"))
                                {
                                    result[symbol] = response[symbol.ToLower()]["usd"];
                                }
                                else
                                {
                                    result[symbol] = 0;
                                }
                            }
                        }
                        break;

                    case "stock":
                    case "etf":
                        // Alpha Vantage niestety nie obsługuje masowych zapytań w darmowym API
                        // Pobierz dane dla każdego symbolu osobno
                        foreach (var symbol in symbols)
                        {
                            result[symbol] = await GetCurrentPriceAsync(symbol, category);
                            // Dodaj opóźnienie, żeby nie przekroczyć limitów API (np. 5 zapytań/min)
                            await Task.Delay(15000); // 15 sekund przerwy między zapytaniami
                        }
                        break;

                    default:
                        // Dla innych kategorii, pobierz ceny pojedynczo
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
                        
                        if (response?.Prices != null)
                        {
                            foreach (var price in response.Prices)
                            {
                                if (price.Count >= 2)
                                {
                                    // CoinGecko zwraca [timestamp, price]
                                    var timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)price[0]).DateTime;
                                    result[timestamp] = price[1];
                                }
                            }
                        }
                        break;

                    case "stock":
                    case "etf":
                        // Alpha Vantage dla danych historycznych
                        endpoint = $"{apiConfig.BaseUrl}/query?function=TIME_SERIES_DAILY&symbol={symbol}&outputsize=full&apikey={apiConfig.ApiKey}";
                        var stockResponse = await client.GetAsync(endpoint);
                        
                        if (stockResponse.IsSuccessStatusCode)
                        {
                            var contentStream = await stockResponse.Content.ReadAsStreamAsync();
                            var options = new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true,
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            };
                            
                            var stockHistorical = await JsonSerializer.DeserializeAsync<AlphaVantageHistorical>(contentStream, options);
                            
                            if (stockHistorical?.TimeSeriesDaily != null)
                            {
                                foreach (var entry in stockHistorical.TimeSeriesDaily)
                                {
                                    if (DateTime.TryParse(entry.Key, out DateTime date))
                                    {
                                        if (date >= startDate && date <= endDate)
                                        {
                                            result[date] = entry.Value.Close;
                                        }
                                    }
                                }
                            }
                        }
                        break;

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

        private async Task<ExternalApiConfig?> GetApiConfigForCategoryAsync(string category)
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
            public required List<List<decimal>> Prices { get; set; } = new();
        }

        private class AlphaVantageQuote
        {
            public GlobalQuoteData? GlobalQuote { get; set; }

            public class GlobalQuoteData
            {
                [System.Text.Json.Serialization.JsonPropertyName("05. price")]
                public decimal Price { get; set; }
            }
        }

        private class AlphaVantageHistorical
        {
            [System.Text.Json.Serialization.JsonPropertyName("Time Series (Daily)")]
            public Dictionary<string, DailyData>? TimeSeriesDaily { get; set; }

            public class DailyData
            {
                [System.Text.Json.Serialization.JsonPropertyName("4. close")]
                public decimal Close { get; set; }
            }
        }
    }
}