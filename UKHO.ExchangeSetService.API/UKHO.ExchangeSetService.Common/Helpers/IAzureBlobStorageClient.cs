using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IAzureBlobStorageClient
    {
        Task<bool> StoreScsResponseAsync(string containerName, string batchId, SalesCatalogueResponse salesCatalogueResponse, CancellationToken cancellationToken);
        CloudBlockBlob GetCloudBlockBlob(string fileName, string storageAccountConnectionString, string containerName);
    }
}
