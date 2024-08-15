using System;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IAzureBlobStorageService
    {
        Task<bool> StoreSaleCatalogueServiceResponseAsync(string containerName, string batchId, SalesCatalogueProductResponse salesCatalogueResponse, string callBackUri, string exchangeSetStandard, string correlationId, CancellationToken cancellationToken, string expiryDate, DateTime scsRequestDateTime, bool isEmptyEncExchangeSet, bool isEmptyAioExchangeSet, ExchangeSetResponse exchangeSetResponse);

        Task<SalesCatalogueProductResponse> DownloadSalesCatalogueResponse(string scsResponseUri, string batchId, string correlationId);

        (string, string) GetStorageAccountNameAndKeyBasedOnExchangeSetType(ExchangeSetType exchangeSetType);

        int GetInstanceCountBasedOnExchangeSetType(ExchangeSetType exchangeSetType);
    }
}