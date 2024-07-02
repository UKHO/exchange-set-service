using System.Collections.Generic;

namespace UKHO.ExchangeSetService.Common.Models.Request
{
    public class ProductDataProductVersionsRequest
    {
        public List<ProductVersionRequest> ProductVersions { get; set; }
        public string CallbackUri { get; set; }
        public string ExchangeSetStandard { get; set; }
        public string CorrelationId { get; set; }
    }
}