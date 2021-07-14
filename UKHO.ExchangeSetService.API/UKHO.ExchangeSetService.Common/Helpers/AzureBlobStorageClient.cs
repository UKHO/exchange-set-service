using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class AzureBlobStorageClient : IAzureBlobStorageClient
    {
        public CloudBlockBlob GetCloudBlockBlob(string fileName, string storageAccountConnectionString, string containerName)
        {
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
            CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);
            CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(fileName);
            return cloudBlockBlob;
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
        public async Task<bool> DeleteFileFromContainer(string storageAccountConnectionString, string containerName, string batchId)
        {
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
            CloudBlobClient _blobClient = cloudStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer _cloudBlobContainer = _blobClient.GetContainerReference(containerName);
            CloudBlockBlob _blockBlob = _cloudBlobContainer.GetBlockBlobReference(batchId);
            bool checkBlobReference = await _blockBlob.ExistsAsync();
            if (checkBlobReference)
            {
                await _blockBlob.DeleteIfExistsAsync();
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}