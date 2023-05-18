using Microsoft.Extensions.Diagnostics.HealthChecks;
using Azure.Storage.Blobs.Specialized;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IAzureBlobStorageClient
    {
        Task<BlockBlobClient> GetCloudBlockBlob(string fileName, string storageAccountConnectionString, string containerName);
        BlockBlobClient GetCloudBlockBlobByUri(string uri, StorageSharedKeyCredential keyCredential);
        Task UploadFromStreamAsync(BlockBlobClient cloudBlockBlob, MemoryStream ms);
        Task<string> DownloadTextAsync(BlockBlobClient cloudBlockBlob);
        Task<HealthCheckResult> CheckBlobContainerHealth(string storageAccountConnectionString, string containerName);
        Task DeleteCacheContainer(string storageAccountConnectionString, string containerName);
        Task<bool> ExistsAsync(BlockBlobClient bbc);
        void CheckUploadCalled();
    }
}