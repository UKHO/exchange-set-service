using Azure.Storage;
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
            var cloudStorageAccount = new BlobServiceClient(storageAccountConnectionString);

            BlobContainerClient cloudBlobContainer = cloudStorageAccount.GetBlobContainerClient(containerName);

            await cloudBlobContainer.CreateIfNotExistsAsync();

            BlockBlobClient cloudBlockBlob = cloudBlobContainer.GetBlockBlobClient(fileName); 
            
            return cloudBlockBlob;
        }

        /// <summary>
        /// This wrapper method used to facilitate FakeItEasy not working with some Storage V12 methods.
        /// </summary>
        /// <param name="bbc"></param>
        /// <returns></returns>
        public async Task<bool> ExistsAsync(BlockBlobClient bbc)
        {
            return await bbc.ExistsAsync();
        }

        /// <summary>
        /// This dummy method used to facilitate FakeItEasy not working with some Storage V12 methods
        /// </summary>
        public void CheckUploadCalled()
        {
            ///does nothing;
        }

        public BlockBlobClient GetCloudBlockBlobByUri(string uri, StorageSharedKeyCredential keyCredential)
        {
            var blockblob = new BlockBlobClient(new Uri(uri),keyCredential);
            return blockblob;
        }

        public async Task UploadFromStreamAsync(BlockBlobClient cloudBlockBlob,MemoryStream ms)
        {
            await cloudBlockBlob.UploadAsync(ms);
        }

        public async Task<string> DownloadTextAsync(BlockBlobClient cloudBlockBlob)
        {
            return await Task.FromResult("Testing");
             //return await cloudBlockBlob.DownloadTextAsync();  //RHZ
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