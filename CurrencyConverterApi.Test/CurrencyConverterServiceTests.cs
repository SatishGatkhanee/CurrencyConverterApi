using CurrencyConverterApi.Providers;
using CurrencyConverterApi.Models;
using CurrencyConverterApi.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using FluentAssertions;

namespace CurrencyConverterApi.Test
{
    public class CurrencyConverterServiceTests
    {
        private readonly Mock<ICurrencyProvider> _mockProvider;
        private readonly CurrencyConverterService _service;

        public CurrencyConverterServiceTests()
        {
            _mockProvider = new Mock<ICurrencyProvider>();
            var mockFactory = new Mock<ICurrencyProviderFactory>();
            mockFactory.Setup(f => f.GetProvider()).Returns(_mockProvider.Object);
            _service = new CurrencyConverterService(mockFactory.Object);
        }

        [Fact]
        public async Task GetLatestRatesAsync_ReturnsExpectedRates()
        {
            var expected = new CurrencyRate { Base = "USD" };
            _mockProvider.Setup(p => p.GetLatestRatesAsync("USD"))
                         .ReturnsAsync(expected);

            var result = await _service.GetLatestRatesAsync("USD");

            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("TRY")]
        [InlineData("PLN")]
        [InlineData("MXN")]
        [InlineData("THB")]
        public async Task ConvertAsync_ShouldThrow_WhenBlockedCurrencyUsed(string blockedCurrency)
        {
            var request = new ConversionRequest
            {
                FromCurrency = blockedCurrency,
                ToCurrency = "USD",
                Amount = 100
            };

            var act = async () => await _service.ConvertAsync(request);

            await act.Should().ThrowAsync<BadHttpRequestException>()
                .WithMessage("Currency not supported");
        }

        [Fact]
        public async Task ConvertAsync_ShouldReturnResult_WhenCurrencyAllowed()
        {
            var request = new ConversionRequest
            {
                FromCurrency = "EUR",
                ToCurrency = "USD",
                Amount = 50
            };

            _mockProvider.Setup(p => p.ConvertAsync("EUR", "USD", 50)).ReturnsAsync(55.5M);

            var result = await _service.ConvertAsync(request);

            result.ConvertedAmount.Should().Be(55.5M);
            result.FromCurrency.Should().Be("EUR");
            result.ToCurrency.Should().Be("USD");
            result.OriginalAmount.Should().Be(50);
            result.RateTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_ShouldReturnPaginatedRates()
        {
            var mockHistory = new HistoryResponse
            {
                Base = "USD",
                Rates = new Dictionary<string, Dictionary<string, decimal>>
                {
                    ["2024-01-01"] = new() { { "EUR", 0.9M }, { "INR", 82 } },
                    ["2024-01-02"] = new() { { "EUR", 0.91M } }
                }
            };

            _mockProvider.Setup(p => p.GetHistoryAsync("USD", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                         .ReturnsAsync(mockHistory);

            var result = await _service.GetHistoricalRatesAsync("USD", DateTime.Today.AddDays(-5), DateTime.Today, 1, 10);

            result.TotalRecords.Should().Be(3);
            result.BaseCurrency.Should().Be("USD");
            result.Page.Should().Be(1);
            result.PageSize.Should().Be(10);
            result.Rates.Count.Should().Be(3);
        }

        [Fact]
        public void PaginateRates_ShouldReturnCorrectSubset()
        {
            var source = new HistoryResponse
            {
                Base = "EUR",
                Rates = new Dictionary<string, Dictionary<string, decimal>>
                {
                    ["2023-01-01"] = new() { { "USD", 1.1M } },
                    ["2023-01-02"] = new() { { "USD", 1.2M } },
                    ["2023-01-03"] = new() { { "USD", 1.3M } },
                }
            };

            var result = _service.PaginateRates(source, 2, 1);

            result.Page.Should().Be(2);
            result.PageSize.Should().Be(1);
            result.TotalRecords.Should().Be(3);
            result.Rates.Single().Rate.Should().Be(1.2M);
        }
    }
}
