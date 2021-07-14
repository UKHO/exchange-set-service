
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IAzureBlobStorageClient
    {
        CloudBlockBlob GetCloudBlockBlob(string fileName, string storageAccountConnectionString, string containerName);
        CloudBlockBlob GetCloudBlockBlobByUri(string uri, string storageAccountConnectionString);
        Task UploadFromStreamAsync(CloudBlockBlob cloudBlockBlob, MemoryStream ms);
        Task<string> DownloadTextAsync(CloudBlockBlob cloudBlockBlob);
        Task DeleteContainerFile(string storageAccountConnectionString, string containerName, string batchId);
    }
}