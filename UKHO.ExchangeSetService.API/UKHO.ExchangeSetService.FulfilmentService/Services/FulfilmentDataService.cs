using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public class FulfilmentDataService : IFulfilmentDataService
    {
        private readonly IScsStorageService scsStorageService;
        private readonly IOptions<EssFulfilmentStorageConfiguration> storageConfig;

        public FulfilmentDataService(IScsStorageService scsStorageService,
                                     IOptions<EssFulfilmentStorageConfiguration> storageConfig)
        {
            this.scsStorageService = scsStorageService;
            this.storageConfig = storageConfig;
        }

        public async Task<string> DownloadSalesCatalogueResponse(string ScsResponseUri, string batchid)
        {
            string storageAccountConnectionString = scsStorageService.GetStorageAccountConnectionString();

            CloudStorageAccount mycloudStorageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
            CloudBlobClient myBlob = mycloudStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer mycontainer = myBlob.GetContainerReference(storageConfig.Value.StorageContainerName);
            CloudBlockBlob myBlockBlob = mycontainer.GetBlockBlobReference(batchid + ".json");

            // provide the location of the file need to be downloaded          
            Stream fileupd = File.OpenWrite(@"D:\Test\" + batchid);

            var responseFile = await myBlockBlob.DownloadTextAsync();
            await myBlockBlob.DownloadToStreamAsync(fileupd);
            Console.WriteLine("Download completed Successfully!!!!");
            return "Download completed Successfully!!!!";

        }
    }
}
