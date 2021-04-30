using Swashbuckle.AspNetCore.Annotations;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.Common.Models.Request
{
    [ExcludeFromCodeCoverage]
    public class ProductDataSinceDateTimeRequest
    {
        [SwaggerSchema(Format = "date-time")]
        public string SinceDateTime { get; set; }

        public string CallbackUri { get; set; }
    }
}
