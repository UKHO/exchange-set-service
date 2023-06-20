using Azure.Storage.Blobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class AzureBlobStorageClient : IAzureBlobStorageClient
    {
        public async Task<BlobClient> GetBlobClient(string fileName, string storageAccountConnectionString, string containerName)
        {
            var blobServiceClient = new BlobServiceClient(storageAccountConnectionString);
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await blobContainerClient.CreateIfNotExistsAsync();
            return blobContainerClient.GetBlobClient(fileName);
        }

        public CloudBlockBlob GetCloudBlockBlobByUri(string uri, string storageAccountConnectionString)
        {
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
            return new CloudBlockBlob(new Uri(uri), cloudStorageAccount.Credentials);
        }

        public async Task UploadFromStreamAsync(CloudBlockBlob cloudBlockBlob,MemoryStream ms)
        {
            await cloudBlockBlob.UploadFromStreamAsync(ms);
        }

        public async Task<string> DownloadTextAsync(CloudBlockBlob cloudBlockBlob)
        {
             return await cloudBlockBlob.DownloadTextAsync();
        }

        public async Task<HealthCheckResult> CheckBlobContainerHealth(string storageAccountConnectionString, string containerName)
        {
            BlobContainerClient container = new BlobContainerClient(storageAccountConnectionString, containerName);
            var blobContainerExists = await container.ExistsAsync();
            if(blobContainerExists)
                return HealthCheckResult.Healthy("Azure blob storage is healthy");
            else
                return HealthCheckResult.Unhealthy("Azure blob storage is unhealthy", new Exception("Azure blob storage connection failed or not available"));
        }

        public async Task DeleteCacheContainer(string storageAccountConnectionString, string containerName)
        {
            BlobContainerClient container = new BlobContainerClient(storageAccountConnectionString, containerName);
            await container.DeleteIfExistsAsync();
        }
    }
}