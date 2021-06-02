using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public class QueryFssService : IQueryFssService
    {
        private readonly IOptions<FileShareServiceConfiguration> fileShareServiceConfig;
        private readonly IFileShareService fileShareService;
        private readonly IAzureBlobStorageClient azureBlobStorageClient;
        private const string CONTENT_TYPE = "application/json";

        public QueryFssService(IOptions<FileShareServiceConfiguration> fileShareServiceConfig, IFileShareService fileShareService, IAzureBlobStorageClient azureBlobStorageClient)
        {
            this.fileShareServiceConfig = fileShareServiceConfig;
            this.fileShareService = fileShareService;
            this.azureBlobStorageClient = azureBlobStorageClient;
        }

        public string GenerateQueryForFss(List<Products> products)
        {
            var productIndex = 1;
            var productCount = products.Count;
            StringBuilder sb = new StringBuilder();
            sb.Append("(");////1st main (
            foreach (var item in products)
            {
                sb.Append("(");////1st product
                sb.AppendFormat(fileShareServiceConfig.Value.CellName, item.ProductName);
                sb.AppendFormat(fileShareServiceConfig.Value.EditionNumber, item.EditionNumber);
                var lstCount = item.UpdateNumbers.Count;
                var index = 1;
                if (item.UpdateNumbers != null && item.UpdateNumbers.Any())
                {
                    foreach (var updateNumberItem in item.UpdateNumbers)
                    {
                        if (index == 1)
                        {
                            sb.Append("((");
                        }
                        sb.AppendFormat(fileShareServiceConfig.Value.UpdateNumber, updateNumberItem.Value);
                        sb.Append(lstCount != index ? "or " : "))");
                        index += 1;
                    }
                }
                sb.Append(productCount == productIndex ? ")" : ") or ");/////last product or with multiple
                productIndex += 1;
            }
            sb.Append(")");//// last main )
            return sb.ToString();
        }

        public List<Products> SliceFssProductsWithUpdateNumber(List<Products> products)
        {
            var listSubUpdateNumberProduts = new List<Products>();
            foreach (var item in products)
            {
                var limit = fileShareServiceConfig.Value.UpdateNumberLimit;
                // assume our list of integers it called values
                var splitByUpdateLimit = item.UpdateNumbers.Aggregate(new List<List<int?>> { new List<int?>() },
                                       (list, value) =>
                                       {
                                           list.Last().Add(value);
                                           if (value >= limit)
                                           {
                                               limit = value.Value + fileShareServiceConfig.Value.UpdateNumberLimit;
                                               list.Add(new List<int?>());
                                           }
                                           return list;
                                       });

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

        public async Task<SearchBatchResponse> QueryFss(List<Products> products)
        {
            var batchProducts = SliceFssProducts(products);
            var listSearchbatchResponse = new List<SearchBatchResponse>();
            foreach (var item in batchProducts)
            {
                listSearchbatchResponse.Add(await fileShareService.GetBatchInfoBasedOnProducts(item));
            }

            var entries = listSearchbatchResponse.Select(a => a.Entries);
            return new SearchBatchResponse() { 
                Entries = entries.FirstOrDefault()
            };
        }

        public IEnumerable<List<Products>> SliceFssProducts(List<Products> products)
        {
            return SplitList((SliceFssProductsWithUpdateNumber(products)), fileShareServiceConfig.Value.ProductLimit);
        }

        public static IEnumerable<List<T>> SplitList<T>(List<T> products, int nSize)
        {
            for (int i = 0; i < products.Count; i += nSize)
            {
                yield return products.GetRange(i, Math.Min(nSize, products.Count - i));
            }
        }

        public async Task<string> UploadFssDataToBlob(string uploadFileName, SearchBatchResponse searchBatchResponse, string storageAccountConnectionString, string containerName)
        {
            var serializeJsonObject = JsonConvert.SerializeObject(searchBatchResponse);

            var cloudBlockBlob = azureBlobStorageClient.GetCloudBlockBlob(uploadFileName, storageAccountConnectionString, containerName);
            cloudBlockBlob.Properties.ContentType = CONTENT_TYPE;

            using (var ms = new MemoryStream())
            {
                LoadStreamWithJson(ms, serializeJsonObject);
                await cloudBlockBlob.UploadFromStreamAsync(ms);
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
    }
}
