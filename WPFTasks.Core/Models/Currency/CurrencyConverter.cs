using Newtonsoft.Json.Linq;
using System.Net.Http;
using TopNetwork.Core;

namespace WPFTasks.Core.Models.Currency
{
    public class CurrencyConverter(LogString? logger = null)
    {
        public static string GetCurrencyTypeStr(CurrencyType type) { return Enum.GetName(typeof(CurrencyType), type); }
        public enum CurrencyType
        {
            USD,
            RUB,
            EUR
        }
        public LogString? Logger { get; set; } = logger;

        public async Task<double?> GetExchangeRate(CurrencyType fromCurrency, CurrencyType toCurrency)
            => await GetExchangeRate(GetCurrencyTypeStr(fromCurrency), GetCurrencyTypeStr(toCurrency));
        public async Task<double?> GetExchangeRate(string fromCurrency, string toCurrency)
        {
            using var httpClient = new HttpClient();

            try
            {
                string apiUrl = "https://api.exchangerate-api.com/v4/latest/" + fromCurrency;  // Бесплатный API
                string response = await httpClient.GetStringAsync(apiUrl);
                JObject data = JObject.Parse(response);
                return data["rates"]?[toCurrency]?.ToObject<double>();
            }
            catch (Exception ex)
            {
                Logger?.Invoke($"Ошибка при получении курса: {ex.Message}");
                return null;
            }
        }
    }
}
