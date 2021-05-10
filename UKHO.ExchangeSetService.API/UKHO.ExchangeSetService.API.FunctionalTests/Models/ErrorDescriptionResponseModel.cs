using Newtonsoft.Json;
using System.Collections.Generic;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Models
{
    /// <summary>
    /// This is Error Description response model class
    /// </summary>
    public class ErrorDescriptionResponseModel
    {
        /// <summary>
        /// Json property CorrelationId
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// Json property Errors
        /// </summary>
        public List<Error> Errors { get; set; }
    }

    /// <summary>
    /// This is Error class
    /// </summary>
    public class Error
    {
        [JsonProperty("source")]
        public string Source { get; set; }
       
        [JsonProperty("description")]
        public string Description { get; set; }
    }
}
