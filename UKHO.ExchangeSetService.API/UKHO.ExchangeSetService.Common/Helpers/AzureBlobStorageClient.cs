using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
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
        public async Task<BlockBlobClient> GetCloudBlockBlob(string fileName, string storageAccountConnectionString, string containerName)
        {
            ///CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(storageAccountConnectionString) RHZ
            var cloudStorageAccount = new BlobServiceClient(storageAccountConnectionString);

            ///CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient()
            ///CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName)
            BlobContainerClient cloudBlobContainer = cloudStorageAccount.GetBlobContainerClient(containerName);

            await cloudBlobContainer.CreateIfNotExistsAsync();

            ///CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(fileName)
            BlockBlobClient cloudBlockBlob = cloudBlobContainer.GetBlockBlobClient(fileName); 
            
            return cloudBlockBlob;
        }

        public BlockBlobClient GetCloudBlockBlobByUri(string uri, string storageAccountConnectionString)
        {
            ///CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(storageAccountConnectionString)
            var cloudStorageAccount = new BlobServiceClient(storageAccountConnectionString);
            return new BlockBlobClient(new Uri(uri), cloudStorageAccount.Credential);
        }

        public async Task UploadFromStreamAsync(BlockBlobClient cloudBlockBlob,MemoryStream ms)
        {
            await cloudBlockBlob.UploadAsync(ms);
        }

        public async Task<string> DownloadTextAsync(BlockBlobClient cloudBlockBlob)
        {
             return await cloudBlockBlob.DownloadTextAsync();  //RHZ
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