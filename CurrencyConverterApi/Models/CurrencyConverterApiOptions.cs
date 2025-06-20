namespace CurrencyConverterApi.Models
{
    public class AppSettings
    {
        public ApiClients ApiClients { get; set; } = new();
        public CacheOptions Cache { get; set; } = new();
        public RateLimitingOptions RateLimiting { get; set; } = new();
    }
    public class ApiClients
    {
        public FrankfurterApiOptions FrankfurterApi { get; set; } = new();
    }
    public class FrankfurterApiOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
    }

    public class CacheOptions
    {
        public int DefaultMinutes { get; set; }
    }

    public class RateLimitingOptions
    {
        public int MaxRequestsPerMinute { get; set; }
    }
}
