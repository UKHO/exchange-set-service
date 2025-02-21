using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.IO;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IAzureBlobStorageClient
    {
        Task<BlobClient> GetBlobClient(string storageAccountConnectionString, string containerName, string fileName);
        //BlobClient GetBlobClientByUri(string uri, StorageSharedKeyCredential keyCredential);
        //Task UploadFromStreamAsync(BlobClient blobClient, MemoryStream ms);
        //Task<string> DownloadTextAsync(BlobClient blobClient);
        Task<string> DownloadTextAsync(string uri, StorageSharedKeyCredential keyCredential);
        Task<string> DownloadTextAsync(string storageAccountConnectionString, string containerName, string fileName);
        Task<bool> DownloadToFileAsync(string storageAccountConnectionString, string containerName, string fileName, string filePath);
        Task<bool> DeleteFile(string storageAccountConnectionString, string containerName, string fileName);
        Task<HealthCheckResult> CheckBlobContainerHealth(string storageAccountConnectionString, string containerName);
        Task DeleteCacheContainer(string storageAccountConnectionString, string containerName);
    }
}
