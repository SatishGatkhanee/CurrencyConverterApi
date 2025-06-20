using CurrencyConverterApi.Models;
using CurrencyConverterApi.Providers;
using CurrencyConverterApi.Services.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CurrencyConverterApi.Services
{
    public class CurrencyConverterService : ICurrencyConverterService
    {
        private readonly ICurrencyProvider _provider;
        public CurrencyConverterService(ICurrencyProviderFactory factory) => _provider = factory.GetProvider();

        public async Task<CurrencyRate> GetLatestRatesAsync(string baseCurrency)
            => await _provider.GetLatestRatesAsync(baseCurrency);

        public async Task<ConversionResult> ConvertAsync(ConversionRequest request)
        {
            var blocked = new[] { "TRY", "PLN", "THB", "MXN" };
            if (blocked.Contains(request.FromCurrency.ToUpper()) || blocked.Contains(request.ToCurrency.ToUpper()))
                throw new BadHttpRequestException("Currency not supported");

            var rate = await _provider.ConvertAsync(request.FromCurrency, request.ToCurrency, request.Amount);

            return new ConversionResult
            {
                FromCurrency = request.FromCurrency,
                ToCurrency = request.ToCurrency,
                OriginalAmount = request.Amount,
                ConvertedAmount = rate,
                RateTimestamp = DateTime.UtcNow
            };
        }

        public async Task<PaginatedHistoryRatesResponse> GetHistoricalRatesAsync(
            string baseCurrency, DateTime start, DateTime end, int page, int pageSize)
        {
            var result = await _provider.GetHistoryAsync(baseCurrency, start, end);
            return PaginateRates(result!, page, pageSize);
        }

        public PaginatedHistoryRatesResponse PaginateRates(HistoryResponse source, int page, int pageSize)
        {
            var allRates = source.Rates
                .SelectMany(dateEntry => dateEntry.Value.Select(currencyRate => new FlattenedRateEntry
                {
                    Date = DateTime.Parse(dateEntry.Key),
                    Currency = currencyRate.Key,
                    Rate = currencyRate.Value
                }))
                .OrderBy(r => r.Date)
                .ToList();

            var paginated = allRates.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new PaginatedHistoryRatesResponse
            {
                BaseCurrency = source.Base,
                Page = page,
                PageSize = pageSize,
                TotalRecords = allRates.Count,
                Rates = paginated
            };
        }
    }
}
