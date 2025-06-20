using CurrencyConverterApi.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace CurrencyConverterApi.Services
{
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;

        public MemoryCacheService(IMemoryCache cache) => _cache = cache;

        public async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null)
        {
            if (_cache.TryGetValue<T>(key, out var cachedValue))
            {
                return cachedValue!;
            }

            var value = await factory();
            _cache.Set(key, value, absoluteExpiration ?? TimeSpan.FromMinutes(10));
            return value;
        }
    }
}
