using Newtonsoft.Json;
using System.Collections.Generic;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Models
{
    public class ErrorDescriptionResponseModel
    {
        public string CorrelationId { get; set; }

        public List<Error> Errors { get; set; }
    }

    public class Error
    {
        [JsonProperty("source")]
        public string Source { get; set; }
       
        [JsonProperty("description")]
        public string Description { get; set; }
    }
}
