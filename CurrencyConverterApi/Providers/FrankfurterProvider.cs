using CurrencyConverterApi.Models;
using CurrencyConverterApi.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace CurrencyConverterApi.Providers
{
    public class FrankfurterProvider : ICurrencyProvider
    {
        private readonly ILogger<FrankfurterProvider> _logger;
        private readonly ICacheService _cacheService;
        private readonly AppSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public FrankfurterProvider(IHttpClientFactory httpClientFactory,
            ILogger<FrankfurterProvider> logger,
            ICacheService cacheService,
            IOptions<AppSettings> options)
        {
            _logger = logger;
            _cacheService = cacheService;
            _httpClient = httpClientFactory.CreateClient("Frankfurter");
            _settings = options.Value;
            _baseUrl = options.Value.ApiClients.FrankfurterApi.BaseUrl;
        }

        public async Task<CurrencyRate> GetLatestRatesAsync(string baseCurrency)
        {
            //var client = httpClientFactory.CreateClient("Frankfurter");

            var cacheKey = $"latest:{baseCurrency}";

            return await _cacheService.GetOrAddAsync(cacheKey, async () =>
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/latest?base={baseCurrency}");
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Called Frankfurter API: {StatusCode} {Url}",
                    response.StatusCode, response.RequestMessage?.RequestUri);

                return await response.Content.ReadFromJsonAsync<CurrencyRate>() ?? throw new Exception("Empty response");
            }, TimeSpan.FromMinutes(_settings.Cache.DefaultMinutes));
        }

        public async Task<decimal> ConvertAsync(string from, string to, decimal amount)
        {
            var cacheKey = $"convert:{from}:{to}:{amount}";

            return await _cacheService.GetOrAddAsync(cacheKey, async () =>
            {
                var url = $"{_baseUrl}/latest?base={from}&symbols={to}&amount={amount}";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Frankfurter API: {StatusCode} {Url}",
                    response.StatusCode, response.RequestMessage?.RequestUri);

                var currencyData = await response.Content.ReadFromJsonAsync<CurrencyRate>();

                if (currencyData?.Rates != null &&
                    currencyData.Rates.TryGetValue(to.ToUpper(), out var rate))
                {
                    return Math.Round(rate);
                }

                throw new Exception($"Rate not found for currency: {to}");
            }, TimeSpan.FromMinutes(_settings.Cache.DefaultMinutes));
        }

        public async Task<HistoryResponse?> GetHistoryAsync(string baseCurrency, DateTime start, DateTime end)
        {
            var cacheKey = $"history:{baseCurrency}:{start:yyyyMMdd}:{end:yyyyMMdd}";

            return await _cacheService.GetOrAddAsync(cacheKey, async () =>
            {
                var url = $"{_baseUrl}/{start:yyyy-MM-dd}..{end:yyyy-MM-dd}?base={baseCurrency}";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Frankfurter API: {StatusCode} {Url}",
                    response.StatusCode, response.RequestMessage?.RequestUri);

                return await response.Content.ReadFromJsonAsync<HistoryResponse>()
                       ?? throw new Exception("Empty response from historical rates");
            }, TimeSpan.FromMinutes(_settings.Cache.DefaultMinutes));
        }
    }
}
