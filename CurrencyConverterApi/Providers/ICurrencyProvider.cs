using CurrencyConverterApi.Models;

namespace CurrencyConverterApi.Providers
{
    public interface ICurrencyProvider
    {
        Task<CurrencyRate> GetLatestRatesAsync(string baseCurrency);
        Task<decimal> ConvertAsync(string from, string to, decimal amount);
        Task<HistoryResponse?> GetHistoryAsync(string baseCurrency, DateTime start, DateTime end);
    }
}
