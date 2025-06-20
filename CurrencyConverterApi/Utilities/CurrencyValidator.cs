namespace CurrencyConverterApi.Utilities
{
    public static class CurrencyValidator
    {
        private static readonly HashSet<string> ValidCurrencies = new() { "USD", "EUR", "GBP", "INR" }; // extend as needed

        public static bool IsValid(string code) => ValidCurrencies.Contains(code.ToUpper());
    }
}
