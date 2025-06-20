using Asp.Versioning;
using CurrencyConverterApi.Models;
using CurrencyConverterApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CurrencyConverterApi.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/exchange")]
    //[Authorize(Roles = "Admin")]
    //[EnableRateLimiting("fixed")]
    public class ExchangeController : ControllerBase
    {
        private readonly ICurrencyConverterService _converter;

        public ExchangeController(ICurrencyConverterService converter) => _converter = converter;

        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestRates([FromQuery] string baseCurrency)
            => Ok(await _converter.GetLatestRatesAsync(baseCurrency));

        [HttpPost("convert")]
        public async Task<IActionResult> Convert([FromBody] ConversionRequest request)
            => Ok(await _converter.ConvertAsync(request));

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory(
            [FromQuery] string baseCurrency,
            [FromQuery] DateTime start,
            [FromQuery] DateTime end,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
            => Ok(await _converter.GetHistoricalRatesAsync(baseCurrency, start, end, page, pageSize));
    }

    
}