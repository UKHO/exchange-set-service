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
        public async Task DeleteContainerFile(string storageAccountConnectionString, string containerName, string batchId)
        {
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
            CloudBlobClient _blobClient = cloudStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer _cloudBlobContainer = _blobClient.GetContainerReference(containerName);
            CloudBlockBlob _blockBlob = _cloudBlobContainer.GetBlockBlobReference(batchId);
            await _blockBlob.DeleteIfExistsAsync();
        }
        public async Task<bool> DeleteDirectoryAsync(string storageAccountConnectionString, string containerName, string filePath)
        {
            Boolean deleteStatus = false;
            if (Directory.Exists(filePath))
            {
                var subFolder = Directory.GetDirectories(filePath);
                foreach (var subFolderName in subFolder)
                {
                    var creation = File.GetCreationTimeUtc(subFolderName);
#pragma warning disable S109 // Magic numbers should not be used
                    if (creation < DateTime.UtcNow.AddMinutes(-5))
#pragma warning restore S109 // Magic numbers should not be used
                    {
                        DirectoryInfo di = new DirectoryInfo(subFolderName);
                        foreach (DirectoryInfo dir in di.GetDirectories())
                        {
                            var batchidFile = dir.Name;
                            string batchId = new DirectoryInfo(batchidFile).Name + ".json";

                            await DeleteContainerFile(storageAccountConnectionString, containerName, batchId);
                            dir.Delete(true);
                        }
                        di.Delete(true);
                        deleteStatus = true;
                    }
                }
                return deleteStatus;
            }
            else
            {
                return deleteStatus;
            }
        }
    }
}
