using Newtonsoft.Json;

namespace UKHO.ExchangeSetService.Common.Models.Response
{
    public class RequestedProductsNotInExchangeSet
    {
        [JsonProperty("productName")]
        public string ProductName { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }
    }
}