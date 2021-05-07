using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.Common.Models.Response
{
    [ExcludeFromCodeCoverage]
    public class InternalServerError
    {
        public string CorrelationId { get; set; }
        public string Detail { get; set; }
    }

}
