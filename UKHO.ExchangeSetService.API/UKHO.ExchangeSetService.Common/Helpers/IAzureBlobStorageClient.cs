using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.IO;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IAzureBlobStorageClient
    {
        Task<BlobClient> GetBlobClient(string fileName, string storageAccountConnectionString, string containerName, bool isExistingBlob = false);
        BlobClient GetBlobClientbByUri(string uri, StorageSharedKeyCredential keyCredential);
        Task UploadFromStreamAsync(BlobClient blobClient, MemoryStream ms);
        Task<string> DownloadTextAsync(BlobClient blobClient);
        Task<HealthCheckResult> CheckBlobContainerHealth(string storageAccountConnectionString, string containerName);
        Task DeleteCacheContainer(string storageAccountConnectionString, string containerName);
    }
}
