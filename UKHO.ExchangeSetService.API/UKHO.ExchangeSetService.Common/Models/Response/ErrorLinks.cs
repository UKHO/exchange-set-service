using Newtonsoft.Json;

namespace UKHO.ExchangeSetService.Common.Models.Response
{
    public class ErrorLinks : Links
    {
        [JsonProperty("errorFileUri")]
        public LinkSetErrorFileUri ExchangeSetErrorFileUri { get; set; }
    }
}