using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class AzureBlobStorageClient : IAzureBlobStorageClient
    {
        public async Task<BlobClient> GetBlobClient(string fileName, string storageAccountConnectionString, string containerName)
        {
            BlobClient blobClient = null;
            var blobServiceClient = new BlobServiceClient(storageAccountConnectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
            if (await blobContainerClient.ExistsAsync())
            {
                blobClient = blobContainerClient.GetBlobClient(fileName);
            }
            return blobClient;
        }

        public BlobClient GetBlobClientByUri(string uri, StorageSharedKeyCredential keyCredential)
        {
            return new BlobClient(new Uri(uri), keyCredential);
        }

        public async Task UploadFromStreamAsync(BlobClient blobClient,MemoryStream ms)
        {
            await blobClient.UploadAsync(ms);
        }

        public async Task<string> DownloadTextAsync(BlobClient blobClient)
        {
            using var ms = new MemoryStream();
            await blobClient.DownloadToAsync(ms);
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        public async Task<HealthCheckResult> CheckBlobContainerHealth(string storageAccountConnectionString, string containerName)
        {
            var blobContainerClient = new BlobContainerClient(storageAccountConnectionString, containerName);
            return await blobContainerClient.ExistsAsync()
                ? HealthCheckResult.Healthy("Azure blob storage is healthy")
                : HealthCheckResult.Unhealthy("Azure blob storage is unhealthy", new Exception("Azure blob storage connection failed or not available"));
        }

        public async Task DeleteCacheContainer(string storageAccountConnectionString, string containerName)
        {
            var blobContainerClient = new BlobContainerClient(storageAccountConnectionString, containerName);
            await blobContainerClient.DeleteIfExistsAsync();
        }
    }
}
