using System.Collections.Generic;

namespace UKHO.ExchangeSetService.Common.Models.Request
{
    public class ProductDataProductVersionsRequest
    {
        public List<ProductVersionRequest> ProductVersions { get; set; }
        public string CallbackUri { get; set; }
        public bool IsUnencrypted { get; set; }
        public string CorrelationId { get; set; }
    }
}
