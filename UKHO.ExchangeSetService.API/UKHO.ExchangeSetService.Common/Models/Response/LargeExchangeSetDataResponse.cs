using System.Collections.Generic;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.Models.Response
{
    public class LargeExchangeSetDataResponse
    {
        public SalesCatalogueProductResponse SalesCatalogueProductResponse { get; set; }
        public SalesCatalogueDataResponse SalesCatalogueDataResponse { get; set; }
        public List<FulfilmentDataResponse> FulfilmentDataResponses { get; set; }
        public string ValidationtFailedMessage { get; set; } = string.Empty;
    }
}
