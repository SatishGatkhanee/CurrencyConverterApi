using System.Text.Json.Serialization;

namespace CurrencyConverterApi.Models
{
    public class HistoryResponse
    {
        public decimal Amount { get; set; }
        public string Base { get; set; }

        [JsonPropertyName("start_date")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("end_date")]
        public DateTime EndDate { get; set; }

        public Dictionary<string, Dictionary<string, decimal>> Rates { get; set; }

    }

    public class PaginatedHistoryRatesResponse
    {
        public string BaseCurrency { get; set; }

        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }

        public List<FlattenedRateEntry> Rates { get; set; }
    }

    public class FlattenedRateEntry
    {
        public DateTime Date { get; set; }
        public string Currency { get; set; }
        public decimal Rate { get; set; }
    }
}
