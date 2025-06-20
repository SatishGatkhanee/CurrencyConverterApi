using CurrencyConverterApi.Models;

namespace CurrencyConverterApi.Services.Interfaces
{
    public interface ICurrencyConverterService
    {
        Task<ConversionResult> ConvertAsync(ConversionRequest request);
        Task<PaginatedHistoryRatesResponse> GetHistoricalRatesAsync(string baseCurrency, DateTime start, DateTime end, int page, int pageSize);
        Task<CurrencyRate> GetLatestRatesAsync(string baseCurrency);
    }
}