using System.Collections.Generic;

namespace UKHO.ExchangeSetService.Common.Models.V2.Request
{
    public class ProductVersionsRequest
    {
        public List<ProductVersionRequest> ProductVersions { get; set; }
        public string CallbackUri { get; set; }
        public string CorrelationId { get; set; }
    }
}
