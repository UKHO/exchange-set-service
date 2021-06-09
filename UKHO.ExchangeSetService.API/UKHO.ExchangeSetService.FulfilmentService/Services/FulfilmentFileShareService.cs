using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public class FulfilmentFileShareService : IFulfilmentFileShareService
    {
        private readonly IOptions<FileShareServiceConfiguration> fileShareServiceConfig;
        private readonly IFileShareService fileShareService;
        private readonly IAzureBlobStorageClient azureBlobStorageClient;
        private const string CONTENT_TYPE = "application/json";

        public FulfilmentFileShareService(IOptions<FileShareServiceConfiguration> fileShareServiceConfig, IFileShareService fileShareService, IAzureBlobStorageClient azureBlobStorageClient)
        {
            this.fileShareServiceConfig = fileShareServiceConfig;
            this.fileShareService = fileShareService;
            this.azureBlobStorageClient = azureBlobStorageClient;
        }

        public List<Products> SliceFileShareServiceProductsWithUpdateNumber(List<Products> products)
        {
            var listSubUpdateNumberProduts = new List<Products>();
            foreach (var item in products)
            {
                var splitByUpdateLimit = SplitList(item.UpdateNumbers, fileShareServiceConfig.Value.UpdateNumberLimit);

                if (splitByUpdateLimit != null && splitByUpdateLimit.Any())
                {
                    foreach (var itemSub in splitByUpdateLimit)
                    {
                        var currentProductSub = new Products();
                        currentProductSub.EditionNumber = item.EditionNumber;
                        currentProductSub.UpdateNumbers = itemSub;
                        currentProductSub.ProductName = item.ProductName;
                        currentProductSub.FileSize = item.FileSize;
                        currentProductSub.Cancellation = item.Cancellation;
                        listSubUpdateNumberProduts.Add(currentProductSub);
                    }
                }
            }
            return listSubUpdateNumberProduts;
        }

        public async Task<List<FulfillmentDataResponse>> QueryFileShareServiceData(List<Products> products)
        {
            if (products != null && products.Any())
            {
                var batchProducts = SliceFileShareServiceProducts(products);
                var listBatchDetails = new List<BatchDetail>();
                foreach (var item in batchProducts)
                {
                    var result = await fileShareService.GetBatchInfoBasedOnProducts(item);
                    listBatchDetails.AddRange(result.Entries);
                }

                return SetFulfillmentDataResponse(new SearchBatchResponse()
                {
                    Entries = listBatchDetails
                }); 
            }
            return null;
        }

        public IEnumerable<List<Products>> SliceFileShareServiceProducts(List<Products> products)
        {
            return SplitList((SliceFileShareServiceProductsWithUpdateNumber(products)), fileShareServiceConfig.Value.ProductLimit);
        }

        public static IEnumerable<List<T>> SplitList<T>(List<T> products, int nSize)
        {
            for (int i = 0; i < products.Count; i += nSize)
            {
                yield return products.GetRange(i, Math.Min(nSize, products.Count - i));
            }
        }

        public async Task<string> UploadFileShareServiceData(string uploadFileName, List<FulfillmentDataResponse> fulfillmentDataResponse, string storageAccountConnectionString, string containerName)
        {
            var serializeJsonObject = JsonConvert.SerializeObject(fulfillmentDataResponse);

            var cloudBlockBlob = azureBlobStorageClient.GetCloudBlockBlob(uploadFileName, storageAccountConnectionString, containerName);
            cloudBlockBlob.Properties.ContentType = CONTENT_TYPE;

            using (var ms = new MemoryStream())
            {
                LoadStreamWithJson(ms, serializeJsonObject);
                await azureBlobStorageClient.UploadFromStreamAsync(cloudBlockBlob, ms);
            }            
            return cloudBlockBlob.Uri.AbsoluteUri;
        }

        private void LoadStreamWithJson(Stream ms, object obj)
        {
            StreamWriter writer = new StreamWriter(ms);
            writer.Write(obj);
            writer.Flush();
            ms.Position = 0;
        }

        private List<FulfillmentDataResponse> SetFulfillmentDataResponse(SearchBatchResponse searchBatchResponse)
        {
            var listFulfilmentData = new List<FulfillmentDataResponse>();
            foreach (var item in searchBatchResponse.Entries)
            {
                listFulfilmentData.Add(new FulfillmentDataResponse { 
                    BatchId = item.BatchId,
                    EditionNumber = Convert.ToInt32(item.Attributes?.Where(a => a.Key == "EditionNumber").Select(b=>b.Value).FirstOrDefault()),
                    ProductName = item.Attributes?.Where(a => a.Key == "CellName").Select(b => b.Value).FirstOrDefault(),
                    UpdateNumber = Convert.ToInt32(item.Attributes?.Where(a => a.Key == "UpdateNumber").Select(b => b.Value).FirstOrDefault()),
                    FileUri = item.Files?.Select(a=>a.Links.Get.Href)
                });
            }
            return listFulfilmentData.OrderBy(a => a.ProductName).ThenBy(b => b.EditionNumber).ThenBy(c => c.UpdateNumber).ToList();
        }
    }
}
