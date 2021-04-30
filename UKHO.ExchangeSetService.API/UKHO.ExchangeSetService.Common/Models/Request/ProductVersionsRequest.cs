using System.Collections.Generic;

namespace UKHO.ExchangeSetService.Common.Models.Request
{
    public class ProductVersionsRequest
    {
        public List<ProductVersionRequest> ProductVersions { get; set; }
        public string CallbackUri { get; set; }
    }
}
