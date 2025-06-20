using CurrencyConverterApi.Models;
using CurrencyConverterApi.Providers;
using CurrencyConverterApi.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace CurrencyConverterApi.Test
{
    public class FrankfurterProviderTests
    {
        private readonly Mock<ICacheService> _mockCache;
        private readonly Mock<ILogger<FrankfurterProvider>> _mockLogger;

        public FrankfurterProviderTests()
        {
            _mockCache = new Mock<ICacheService>();
            _mockLogger = new Mock<ILogger<FrankfurterProvider>>();
        }

        #region Private methods
        private FrankfurterProvider CreateProvider(HttpResponseMessage response)
        {
            return new FrankfurterProvider(GetMockHttpClientFactory(response).Object,
                _mockLogger.Object, _mockCache.Object, GetMockOptions().Object);
        }

        private static Mock<IHttpClientFactory> GetMockHttpClientFactory(HttpResponseMessage response)
        {
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            var mockHttpClient = new HttpClient(mockHandler.Object)
            {
                BaseAddress = new Uri(GetAppSetings().ApiClients.FrankfurterApi.BaseUrl)
            };

            //Mock IHttpClientFactory
            var mockFactory = new Mock<IHttpClientFactory>();
            mockFactory.Setup(f => f.CreateClient("Frankfurter")).Returns(mockHttpClient);
            return mockFactory;
        }

        private static Mock<IOptions<AppSettings>> GetMockOptions()
        {
            AppSettings appSettings = GetAppSetings();
            var mockOptions = new Mock<IOptions<AppSettings>>();
            mockOptions.Setup(x => x.Value).Returns(appSettings);
            return mockOptions;
        }

        private static AppSettings GetAppSetings()
        {
            return new AppSettings
            {
                ApiClients = new ApiClients
                {
                    FrankfurterApi = new FrankfurterApiOptions
                    {
                        BaseUrl = "https://api.frankfurter.dev/v1/"
                    }
                },
                Cache = new CacheOptions
                {
                    DefaultMinutes = 10
                },
                RateLimiting = new RateLimitingOptions
                {
                    MaxRequestsPerMinute = 100
                }
            };
        }

        #endregion

        #region Public methods

        [Fact]
        public async Task GetLatestRatesAsync_Returns_From_Cache_When_Available()
        {
            var expected = new CurrencyRate { Base = "USD" };
            _mockCache
                .Setup(x => x.GetOrAddAsync("latest:USD", It.IsAny<Func<Task<CurrencyRate>>>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(expected);

            var provider = CreateProvider(new HttpResponseMessage());
            var result = await provider.GetLatestRatesAsync("USD");

            result.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task ConvertAsync_Returns_Rounded_Rate_From_Cache()
        {
            _mockCache
                .Setup(x => x.GetOrAddAsync("convert:USD:INR:100", It.IsAny<Func<Task<decimal>>>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(82);

            var provider = CreateProvider(new HttpResponseMessage());
            var result = await provider.ConvertAsync("USD", "INR", 100);

            result.Should().Be(82);
        }

        [Fact]
        public async Task GetHistoryAsync_Returns_History_From_Cache()
        {
            var expected = new HistoryResponse
            {
                Base = "USD",
                Rates = new Dictionary<string, Dictionary<string, decimal>>
                {
                    ["2024-01-01"] = new() { { "EUR", 0.9M } }
                }
            };

            var start = new DateTime(2024, 01, 01);
            var end = new DateTime(2024, 01, 10);
            var cacheKey = $"history:USD:{start:yyyyMMdd}:{end:yyyyMMdd}";

            _mockCache
                .Setup(x => x.GetOrAddAsync(cacheKey, It.IsAny<Func<Task<HistoryResponse>>>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(expected);

            var provider = CreateProvider(new HttpResponseMessage());
            var result = await provider.GetHistoryAsync("USD", start, end);

            result!.Base.Should().Be("USD");
            result.Rates.Should().ContainKey("2024-01-01");
        }
    }
    #endregion
}
