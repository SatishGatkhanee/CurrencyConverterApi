namespace CurrencyConverterApi.Services.Interfaces
{
    public interface ICacheService
    {
        Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null);
    }
}