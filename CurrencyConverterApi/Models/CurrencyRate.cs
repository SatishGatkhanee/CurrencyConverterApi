namespace CurrencyConverterApi.Models
{
    public class CurrencyRate
    {
        public string Base { get; set; }
        public DateTime Date { get; set; }
        public Dictionary<string, decimal> Rates { get; set; }
        public decimal Amount { get; set; }

    }
}
