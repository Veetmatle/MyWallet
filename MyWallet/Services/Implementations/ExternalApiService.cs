using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using MyWallet.Data;
using MyWallet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace MyWallet.Services.Implementations
{
    /// <summary>
    ///  Pobiera ceny papierów/krypto przez zewnętrzne API.
    ///  – CoinGecko (kryptowaluty)
    ///  – AlphaVantage (akcje / ETF)
    /// </summary>
    public class ExternalApiService : IExternalApiService
    {
        private const int CACHE_SECONDS = 30;           // minimalny okres odświeżania – pasuje do limitu 30 req/min

        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory  _clientFactory;
        private readonly ILogger<ExternalApiService> _logger;
        private readonly IMemoryCache        _cache;

        public ExternalApiService(
            ApplicationDbContext context,
            IHttpClientFactory clientFactory,
            ILogger<ExternalApiService> logger,
            IMemoryCache cache)
        {
            _context       = context;
            _clientFactory = clientFactory;
            _logger        = logger;
            _cache         = cache;
        }

        #region PUBLIC API --------------------------------------------------

        public async Task<decimal> GetCurrentPriceAsync(string symbol, string category)
        {
            var cacheKey = $"price:{category}:{symbol}".ToLower();
            if (_cache.TryGetValue(cacheKey, out decimal cachedPrice))
                return cachedPrice;

            try
            {
                var apiConfig = await GetApiConfigForCategoryAsync(category);
                if (apiConfig == null)
                {
                    _logger.LogError("No API configuration found for category: {Category}", category);
                    return 0m;
                }

                var client   = _clientFactory.CreateClient();
                ConfigureClient(client, apiConfig, category);

                decimal fetchedPrice = await FetchSinglePriceAsync(client, apiConfig, symbol, category);

                // cache nawet jeśli 0, żeby nie spamować API przy nieznanych symbolach
                _cache.Set(cacheKey, fetchedPrice, TimeSpan.FromSeconds(CACHE_SECONDS));
                return fetchedPrice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching price for {Symbol} in category {Category}", symbol, category);
                return 0m;
            }
        }

        public async Task<Dictionary<string, decimal>> GetMultipleCurrentPricesAsync(IEnumerable<string> symbols, string category)
        {
            var result = new Dictionary<string, decimal>();
            var symbolsList = symbols as IList<string> ?? symbols.ToList();

            // Sprawdź cache – zbierz te, które trzeba jeszcze pobrać
            var missing = new List<string>();
            foreach (var s in symbolsList)
            {
                var ckey = $"price:{category}:{s}".ToLower();
                if (_cache.TryGetValue(ckey, out decimal price))
                {
                    result[s] = price;
                }
                else
                {
                    missing.Add(s);
                }
            }
            if (!missing.Any())
                return result;   // wszystko w cache

            try
            {
                var apiConfig = await GetApiConfigForCategoryAsync(category);
                if (apiConfig == null)
                {
                    _logger.LogError("No API configuration found for category: {Category}", category);
                    return result;
                }

                var client = _clientFactory.CreateClient();
                ConfigureClient(client, apiConfig, category);

                switch (category.ToLower())
                {
                    case "cryptocurrency":
                        await FetchCryptoBulkAsync(client, apiConfig, missing, result);
                        break;

                    case "stock":
                    case "etf":
                        foreach (var s in missing)
                        {
                            // AlphaVantage – brak endpointu bulk w darmowym planie
                            result[s] = await GetCurrentPriceAsync(s, category);
                            await Task.Delay(TimeSpan.FromSeconds(15)); // limit 5 req/min
                        }
                        break;

                    default:
                        foreach (var s in missing)
                            result[s] = await GetCurrentPriceAsync(s, category);
                        break;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching multiple prices for category {Category}", category);
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
                    _logger.LogError("No API configuration found for category: {Category}", category);
                    return result;
                }

                var client = _clientFactory.CreateClient();
                ConfigureClient(client, apiConfig, category);

                switch (category.ToLower())
                {
                    case "cryptocurrency":
                        await FetchCryptoHistoryAsync(client, apiConfig, symbol, startDate, endDate, result);
                        break;

                    case "stock":
                    case "etf":
                        await FetchAlphaVantageHistoryAsync(client, apiConfig, symbol, startDate, endDate, result);
                        break;

                    default:
                        _logger.LogWarning("Historical data not implemented for category: {Category}", category);
                        break;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching historical prices for {Symbol} in category {Category}", symbol, category);
                return result;
            }
        }

        public async Task<IEnumerable<AssetHintDto>> SearchAssetsAsync(string query, string category)
        {
            try
            {
                var apiConfig = await GetApiConfigForCategoryAsync(category);
                if (apiConfig == null || string.IsNullOrWhiteSpace(query))
                    return Enumerable.Empty<AssetHintDto>();

                var client = _clientFactory.CreateClient();
                ConfigureClient(client, apiConfig, category);

                switch (category.ToLower())
                {
                    case "stock":
                    case "etf":
                        var url = $"{apiConfig.BaseUrl}/query?function=SYMBOL_SEARCH&keywords={Uri.EscapeDataString(query)}&apikey={apiConfig.ApiKey}";
                        var resp = await client.GetFromJsonAsync<AlphaVantageSearchResponse>(url);
                        return resp?.BestMatches?.Select(m => new AssetHintDto { Symbol = m.Symbol, Name = m.Name })
                               ?? Enumerable.Empty<AssetHintDto>();

                    case "cryptocurrency":
                        // Pobierz pełną listę i filtruj lokalnie - prostsze i bardziej niezawodne
                        var listUrl = $"{apiConfig.BaseUrl}/coins/list";
                        var coins = await client.GetFromJsonAsync<List<CoinListItem>>(listUrl);
                        
                        if (coins == null || !coins.Any())
                        {
                            _logger.LogWarning("No coins returned from CoinGecko API");
                            return Enumerable.Empty<AssetHintDto>();
                        }

                        return coins
                            .Where(c => !string.IsNullOrEmpty(c.Id) && !string.IsNullOrEmpty(c.Name) &&
                                       (c.Id.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                                        c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                        (!string.IsNullOrEmpty(c.Symbol) && c.Symbol.Contains(query, StringComparison.OrdinalIgnoreCase))))
                            .OrderBy(c => c.Name)
                            .Take(20)
                            .Select(c => new AssetHintDto 
                            { 
                                Symbol = c.Id, 
                                Name = $"{c.Name} ({c.Symbol?.ToUpper()})" 
                            });

                    default:
                        return Enumerable.Empty<AssetHintDto>();
                }

                return Enumerable.Empty<AssetHintDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching assets for category {Category} with query {Query}", category, query);
                return Enumerable.Empty<AssetHintDto>();
            }
        }

        #endregion

        #region PRIVATE HELPERS -------------------------------------------

        private async Task<decimal> FetchSinglePriceAsync(HttpClient client, ExternalApiConfig apiConfig, string symbol, string category)
        {
            switch (category.ToLower())
            {
                case "cryptocurrency":
                    var cryptoEndpoint = $"{apiConfig.BaseUrl}/simple/price?ids={symbol.ToLower()}&vs_currencies=usd";
                    var cryptoResponse = await client.GetFromJsonAsync<Dictionary<string, Dictionary<string, decimal>>>(cryptoEndpoint);
                    if (cryptoResponse != null &&
                        cryptoResponse.TryGetValue(symbol.ToLower(), out var priceDict) &&
                        priceDict.TryGetValue("usd", out var price))
                    {
                        return price;
                    }
                    return 0m;

                case "stock":
                case "etf":
                    var endpoint = $"{apiConfig.BaseUrl}/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={apiConfig.ApiKey}";
                    var response = await client.GetAsync(endpoint);
                    if (!response.IsSuccessStatusCode)
                        return 0m;

                    var contentStream = await response.Content.ReadAsStreamAsync();
                    var stockQuote    = await JsonSerializer.DeserializeAsync<AlphaVantageQuote>(contentStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return stockQuote?.GlobalQuote?.Price ?? 0m;

                default:
                    return 0m;
            }
        }

        private async Task FetchCryptoBulkAsync(HttpClient client, ExternalApiConfig apiConfig, IList<string> symbols, Dictionary<string, decimal> dest)
        {
            var ids = string.Join(',', symbols.Select(s => s.ToLower()));
            var url = $"{apiConfig.BaseUrl}/simple/price?ids={ids}&vs_currencies=usd";
            var response = await client.GetFromJsonAsync<Dictionary<string, Dictionary<string, decimal>>>(url);
            if (response == null) return;

            foreach (var s in symbols)
            {
                var key = s.ToLower();
                decimal price = response.TryGetValue(key, out var dict) && dict.TryGetValue("usd", out var p) ? p : 0m;
                dest[s] = price;
                _cache.Set($"price:cryptocurrency:{s}".ToLower(), price, TimeSpan.FromSeconds(CACHE_SECONDS));
            }
        }

        private async Task FetchCryptoHistoryAsync(HttpClient client, ExternalApiConfig apiConfig, string symbol, DateTime startDate, DateTime endDate, Dictionary<DateTime, decimal> dest)
        {
            var fromTs = ((DateTimeOffset)startDate).ToUnixTimeSeconds();
            var toTs   = ((DateTimeOffset)endDate).ToUnixTimeSeconds();

            var url = $"{apiConfig.BaseUrl}/coins/{symbol.ToLower()}/market_chart/range?vs_currency=usd&from={fromTs}&to={toTs}";
            var response = await client.GetFromJsonAsync<CryptoHistoricalResponse>(url);
            if (response?.Prices == null) return;

            foreach (var p in response.Prices)
            {
                if (p.Count < 2) continue;
                var date = DateTimeOffset.FromUnixTimeMilliseconds((long)p[0]).DateTime.Date;
                dest[date] = p[1];
            }
        }

        private async Task FetchAlphaVantageHistoryAsync(HttpClient client, ExternalApiConfig apiConfig, string symbol, DateTime startDate, DateTime endDate, Dictionary<DateTime, decimal> dest)
        {
            var url = $"{apiConfig.BaseUrl}/query?function=TIME_SERIES_DAILY&symbol={symbol}&outputsize=full&apikey={apiConfig.ApiKey}";
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return;

            var contentStream = await response.Content.ReadAsStreamAsync();
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var hist = await JsonSerializer.DeserializeAsync<AlphaVantageHistorical>(contentStream, opts);
            if (hist?.TimeSeriesDaily == null) return;

            foreach (var entry in hist.TimeSeriesDaily)
            {
                if (!DateTime.TryParse(entry.Key, out var date)) continue;
                if (date >= startDate && date <= endDate)
                    dest[date] = entry.Value.Close;
            }
        }

        private void ConfigureClient(HttpClient client, ExternalApiConfig apiConfig, string category)
        {
            switch (category.ToLower())
            {
                case "cryptocurrency":
                    // CoinGecko wymaga klucza w nagłówku
                    client.DefaultRequestHeaders.Remove("x-cg-demo-api-key");
                    client.DefaultRequestHeaders.Add("x-cg-demo-api-key", apiConfig.ApiKey);
                    if (!client.DefaultRequestHeaders.Contains("User-Agent"))
                        client.DefaultRequestHeaders.Add("User-Agent", "MyWalletApp/1.0");
                    break;
            }
        }

        private async Task<ExternalApiConfig?> GetApiConfigForCategoryAsync(string category) =>
            await _context.ExternalApiConfigs.FirstOrDefaultAsync(c => c.Name.ToLower() == category.ToLower() && c.IsActive);

        #endregion

        #region DTOs for deserialization -----------------------------------

        private class CryptoHistoricalResponse
        {
            public required List<List<decimal>> Prices { get; set; } = new();
        }

        private class AlphaVantageQuote
        {
            public GlobalQuoteData? GlobalQuote { get; set; }
            public class GlobalQuoteData
            {
                [JsonPropertyName("05. price")] public decimal Price { get; set; }
            }
        }

        private class AlphaVantageHistorical
        {
            [JsonPropertyName("Time Series (Daily)")] public Dictionary<string, DailyData>? TimeSeriesDaily { get; set; }
            public class DailyData
            {
                [JsonPropertyName("4. close")] public decimal Close { get; set; }
            }
        }

        private class AlphaVantageSearchResponse
        {
            [JsonPropertyName("bestMatches")] public List<AlphaMatch>? BestMatches { get; set; }
        }

        private class AlphaMatch
        {
            [JsonPropertyName("1. symbol")] public string Symbol { get; set; } = string.Empty;
            [JsonPropertyName("2. name"  )] public string Name   { get; set; } = string.Empty;
        }

        private class CoinListItem
        {
            public string Id     { get; set; } = string.Empty;
            public string Symbol { get; set; } = string.Empty;
            public string Name   { get; set; } = string.Empty;
        }

        private class StockResponse { public decimal CurrentPrice { get; set; } }

        #endregion
    }
}
