using Microsoft.Extensions.Options;
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

        public FulfilmentFileShareService(IOptions<FileShareServiceConfiguration> fileShareServiceConfig, 
            IFileShareService fileShareService)
        {
            this.fileShareServiceConfig = fileShareServiceConfig;
            this.fileShareService = fileShareService;
        }

        public List<Products> SliceFileShareServiceProductsWithUpdateNumber(List<Products> products)
        {
            var listSubUpdateNumberProduts = new List<Products>();
            foreach (var item in products)
            {
                var splitByUpdateLimit = CommonHelper.SplitList(item.UpdateNumbers, fileShareServiceConfig.Value.UpdateNumberLimit);

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

        public async Task<List<FulfilmentDataResponse>> QueryFileShareServiceData(List<Products> products, string correlationId)
        {
            if (products != null && products.Any())
            {
                var batchProducts = SliceFileShareServiceProducts(products);
                var listBatchDetails = new List<BatchDetail>();
                int fileShareServiceSearchQueryCount = 0;
                foreach (var item in batchProducts)
                {
                    var result = await fileShareService.GetBatchInfoBasedOnProducts(item, correlationId);
                    listBatchDetails.AddRange(result.Entries);
                    fileShareServiceSearchQueryCount += result.QueryCount;
                }              
                var fulFilmentDataResponse = SetFulfilmentDataResponse(new SearchBatchResponse()
                {
                    Entries = listBatchDetails,

                });
                if (fulFilmentDataResponse.Count > 0)
                    fulFilmentDataResponse.FirstOrDefault().FileShareServiceSearchQueryCount = fileShareServiceSearchQueryCount;
                return fulFilmentDataResponse;
            }
            return null;
        }

        public async Task DownloadFileShareServiceFiles(SalesCatalogueServiceResponseQueueMessage message, List<FulfilmentDataResponse> fulfilmentDataResponse, string exchangeSetRootPath)
        {
            foreach (var item in fulfilmentDataResponse)
            {
                var downloadPath = Path.Combine(exchangeSetRootPath, item.ProductName.Substring(0, 2), item.ProductName, Convert.ToString(item.EditionNumber), Convert.ToString(item.UpdateNumber));
                await fileShareService.DownloadBatchFiles(item.FileUri, downloadPath, message.CorrelationId);
            }
        }

        public IEnumerable<List<Products>> SliceFileShareServiceProducts(List<Products> products)
        {
            return CommonHelper.SplitList((SliceFileShareServiceProductsWithUpdateNumber(products)), fileShareServiceConfig.Value.ProductLimit);
        }

        private List<FulfilmentDataResponse> SetFulfilmentDataResponse(SearchBatchResponse searchBatchResponse)
        {
            var listFulfilmentData = new List<FulfilmentDataResponse>();
            foreach (var item in searchBatchResponse.Entries)
            {
                listFulfilmentData.Add(new FulfilmentDataResponse { 
                    BatchId = item.BatchId,
                    EditionNumber = Convert.ToInt32(item.Attributes?.Where(a => a.Key == "EditionNumber").Select(b=>b.Value).FirstOrDefault()),
                    ProductName = item.Attributes?.Where(a => a.Key == "CellName").Select(b => b.Value).FirstOrDefault(),
                    UpdateNumber = Convert.ToInt32(item.Attributes?.Where(a => a.Key == "UpdateNumber").Select(b => b.Value).FirstOrDefault()),
                    FileUri = item.Files?.Select(a=>a.Links.Get.Href),
                    Files = item.Files
                });
            }
            return listFulfilmentData.OrderBy(a => a.ProductName).ThenBy(b => b.EditionNumber).ThenBy(c => c.UpdateNumber).ToList();
        }
        public async Task<bool> DownloadReadMeFile(string filePath, string batchId, string exchangeSetRootPath, string correlationId)
        {
           return await fileShareService.DownloadReadMeFile(filePath, batchId, exchangeSetRootPath, correlationId);            
        }
        public async Task<string> SearchReadMeFilePath(string batchId, string correlationId)
        {
           return await fileShareService.SearchReadMeFilePath(batchId, correlationId);
        }
        public bool CreateZipFileForExchangeSet(string batchId, string exchangeSetZipRootPath, string correlationId)
        {
            return fileShareService.CreateZipFileForExchangeSet(batchId, exchangeSetZipRootPath, correlationId);
        }
        public async Task<bool> UploadZipFileForExchangeSetToFileShareService(string batchId, string exchangeSetZipRootPath, string correlationId)
        {
            return await fileShareService.UploadZipFileForExchangeSetToFileShareService(batchId, exchangeSetZipRootPath, correlationId);
        }
    }
}