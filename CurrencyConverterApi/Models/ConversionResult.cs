namespace CurrencyConverterApi.Models
{
    public class ConversionResult
    {
        public string FromCurrency { get; set; }
        public string ToCurrency { get; set; }
        public decimal OriginalAmount { get; set; }
        public decimal ConvertedAmount { get; set; }
        public DateTime RateTimestamp { get; set; }
    }
}
