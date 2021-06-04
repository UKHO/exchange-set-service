using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IAzureBlobStorageClient
    {
        CloudBlockBlob GetCloudBlockBlob(string fileName, string storageAccountConnectionString, string containerName);
        Task<bool> StoreSaleCatalogueServiceResponseAsync(string containerName, string batchId, SalesCatalogueProductResponse salesCatalogueResponse, string callBackUri, string correlationId, CancellationToken cancellationToken);
        Task<SalesCatalogueProductResponse> DownloadSalesCatalogueResponse(string uri);
    }
}
