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
        public async Task<BlobClient> GetBlobClient(string storageAccountConnectionString, string containerName, string fileName)
        {
            var blobServiceClient = new BlobServiceClient(storageAccountConnectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
            if (!await blobContainerClient.ExistsAsync())
            {
                await blobContainerClient.CreateAsync();
            }
                
            return blobContainerClient.GetBlobClient(fileName);
        }

        public async Task<string> DownloadTextAsync(string storageAccountConnectionString, string containerName, string fileName)
        {
            var blobClient = new BlobClient(storageAccountConnectionString, containerName,fileName);
            return await InternalDownloadTextAsync(blobClient);
        }

        public async Task<string> DownloadTextAsync(string uri, StorageSharedKeyCredential keyCredential)
        {
            var blobClient = new BlobClient(new Uri(uri), keyCredential);
            return await InternalDownloadTextAsync(blobClient);
        }

        internal async Task<string> InternalDownloadTextAsync(BlobClient blobClient)
        {
            if (await blobClient.ExistsAsync())
            {
                using var ms = new MemoryStream();
                await blobClient.DownloadToAsync(ms);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
            else
            {
                //what should happen here?
                return string.Empty;
            }
        }

        public async Task<bool> DownloadToFileAsync(string storageAccountConnectionString, string containerName, string fileName, string filePath)
        {
            var blobClient = new BlobClient(storageAccountConnectionString, containerName, fileName);
            if (await blobClient.ExistsAsync())
            {
                await blobClient.DownloadToAsync(filePath);
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<HealthCheckResult> CheckBlobContainerHealth(string storageAccountConnectionString, string containerName)
        {
            var blobContainerClient = new BlobContainerClient(storageAccountConnectionString, containerName);
            return await blobContainerClient.ExistsAsync()
                ? HealthCheckResult.Healthy("Azure blob storage is healthy")
                : HealthCheckResult.Unhealthy("Azure blob storage is unhealthy", new Exception("Azure blob storage connection failed or not available"));
        }

        public async Task<bool> DeleteFile(string storageAccountConnectionString, string containerName, string fileName)
        {
            var blobClient = new BlobClient(storageAccountConnectionString, containerName, fileName);
            return await blobClient.DeleteIfExistsAsync();
        }

        public async Task DeleteCacheContainer(string storageAccountConnectionString, string containerName)
        {
            var blobContainerClient = new BlobContainerClient(storageAccountConnectionString, containerName);
            await blobContainerClient.DeleteIfExistsAsync();
        }

        public BlobClient GetBlobClientByUri(string uri, StorageSharedKeyCredential keyCredential)
        {
            return new BlobClient(new Uri(uri), keyCredential);
        }
    }
}
