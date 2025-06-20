using CurrencyConverterApi.Providers;

namespace CurrencyConverterApi.Services
{
    public interface ICurrencyProviderFactory
    {
        ICurrencyProvider GetProvider();
    }
    public class CurrencyProviderFactory : ICurrencyProviderFactory
    {
        private readonly ICurrencyProvider _provider;

        public CurrencyProviderFactory(ICurrencyProvider provider) => _provider = provider;

        public virtual ICurrencyProvider GetProvider() => _provider; // Expand for multiple providers
    }
}
