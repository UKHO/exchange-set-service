﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Helpers.Zip;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public class FulfilmentFileShareService(
        IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
        IFileShareBatchService fileShareBatchService,
        IFileShareDownloadService fileShareDownloadService,
        IFileShareSearchService fileShareSearchService,
        IFileShareUploadService fileShareUploadService,
        IZip zip,
        ILogger<FulfilmentFileShareService> logger) : IFulfilmentFileShareService
    {
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
                        currentProductSub.Bundle = item.Bundle;
                        listSubUpdateNumberProduts.Add(currentProductSub);
                    }
                }
            }
            return listSubUpdateNumberProduts;
        }

        public async Task<List<FulfilmentDataResponse>> QueryFileShareServiceData(List<Products> products, SalesCatalogueServiceResponseQueueMessage message, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken, string exchangeSetRootPath, string businessUnit)
        {
            if (products != null && products.Any())
            {
                var batchProducts = SliceFileShareServiceProducts(products);
                var listBatchDetails = new List<BatchDetail>();
                int fileShareServiceSearchQueryCount = 0;
                foreach (var item in batchProducts)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        var productDetail = new StringBuilder();
                        foreach (var productitem in item)
                        {
                            productDetail.AppendFormat("\n Product/CellName:{0}, EditionNumber:{1} and UpdateNumbers:[{2}]", productitem.ProductName, productitem.EditionNumber.ToString(), string.Join(",", productitem?.UpdateNumbers.Select(a => a.Value.ToString())));
                        }
                        logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation cancelled as IsCancellationRequested flag is true while searching ENC files from File Share Service with cancellationToken:{cancellationTokenSource.Token} at time:{DateTime.UtcNow} and productdetails:{productDetail.ToString()} and BatchId:{batchId} and _X-Correlation-ID:{correlationId}", JsonConvert.SerializeObject(cancellationTokenSource.Token), DateTime.UtcNow, productDetail.ToString(), message.BatchId, message.CorrelationId);
                        throw new OperationCanceledException();
                    }
                    var result = await fileShareBatchService.GetBatchInfoBasedOnProducts(item, message, cancellationTokenSource, cancellationToken, exchangeSetRootPath, businessUnit);
                    listBatchDetails.AddRange(result.Entries);
                    fileShareServiceSearchQueryCount += result.QueryCount;
                }

                var fulFilmentDataResponse = SetFulfilmentDataResponse(new SearchBatchResponse()
                {
                    Entries = listBatchDetails
                });
                if (fulFilmentDataResponse.Count > 0)
                    fulFilmentDataResponse.FirstOrDefault().FileShareServiceSearchQueryCount = fileShareServiceSearchQueryCount;
                return fulFilmentDataResponse;
            }
            return null;
        }

        public IEnumerable<List<Products>> SliceFileShareServiceProducts(List<Products> products)
        {
            return CommonHelper.SplitList(SliceFileShareServiceProductsWithUpdateNumber(products), fileShareServiceConfig.Value.ProductLimit);
        }

        private List<FulfilmentDataResponse> SetFulfilmentDataResponse(SearchBatchResponse searchBatchResponse)
        {
            var listFulfilmentData = new List<FulfilmentDataResponse>();
            foreach (var item in searchBatchResponse.Entries)
            {
                listFulfilmentData.Add(new FulfilmentDataResponse
                {
                    BatchId = item.BatchId,
                    EditionNumber = Convert.ToInt32(item.Attributes?.Where(a => a.Key == "EditionNumber").Select(b => b.Value).FirstOrDefault()),
                    ProductName = item.Attributes?.Where(a => a.Key == "CellName").Select(b => b.Value).FirstOrDefault(),
                    UpdateNumber = Convert.ToInt32(item.Attributes?.Where(a => a.Key == "UpdateNumber").Select(b => b.Value).FirstOrDefault()),
                    FileUri = item.Files?.Select(a => a.Links.Get.Href),
                    Files = item.Files
                });
            }
            return listFulfilmentData.OrderBy(a => a.ProductName).ThenBy(b => b.EditionNumber).ThenBy(c => c.UpdateNumber).ToList();
        }
        public async Task<bool> DownloadReadMeFileFromFssAsync(string filePath, string batchId, string exchangeSetRootPath, string correlationId)
        {
            return await fileShareDownloadService.DownloadReadMeFileFromFssAsync(filePath, batchId, exchangeSetRootPath, correlationId);
        }

        public async Task<string> SearchReadMeFilePath(string batchId, string correlationId)
        {
            return await fileShareSearchService.SearchReadMeFilePath(batchId, correlationId);
        }

        public async Task<string> SearchIhoPubFilePath(string batchId, string correlationId)
        {
            return await fileShareSearchService.SearchIhoPubFilePath(batchId, correlationId);
        }

        public async Task<string> SearchIhoCrtFilePath(string batchId, string correlationId)
        {
            return await fileShareSearchService.SearchIhoCrtFilePath(batchId, correlationId);
        }
        public async Task<bool> DownloadIhoPubFile(string filePath, string batchId, string exchangeSetRootPath, string correlationId)
        {
            return await fileShareDownloadService.DownloadIhoPubFile(filePath, batchId, exchangeSetRootPath, correlationId);
        }

        public async Task<bool> DownloadIhoCrtFile(string filePath, string batchId, string aioExchangeSetPath, string correlationId)
        {
            return await fileShareDownloadService.DownloadIhoCrtFile(filePath, batchId, aioExchangeSetPath, correlationId);
        }

        public async Task<bool> CreateZipFileForExchangeSet(string batchId, string exchangeSetZipRootPath, string correlationId)
        {
            return await zip.CreateZipFileForExchangeSet(batchId, exchangeSetZipRootPath, correlationId);
        }

        public async Task<bool> UploadZipFileForExchangeSetToFileShareService(string batchId, string exchangeSetZipRootPath, string correlationId, string zipFileName)
        {
            return await fileShareUploadService.UploadFileToFileShareService(batchId, exchangeSetZipRootPath, correlationId, zipFileName);
        }

        public async Task<bool> UploadZipFileForLargeMediaExchangeSetToFileShareService(string batchId, string exchangeSetZipRootPath, string correlationId, string mediaZipFileName)
        {
            return await fileShareUploadService.UploadLargeMediaFileToFileShareService(batchId, exchangeSetZipRootPath, correlationId, mediaZipFileName);
        }

        public async Task<bool> CommitLargeMediaExchangeSet(string batchId, string exchangeSetZipPath, string correlationId)
        {
            return await fileShareBatchService.CommitAndGetBatchStatusForLargeMediaExchangeSet(batchId, exchangeSetZipPath, correlationId);
        }

        public async Task<IEnumerable<BatchFile>> SearchFolderDetails(string batchId, string correlationId, string folderName)
        {
            string uri = $"{fileShareServiceConfig.Value.BaseUrl}/batch?$filter=$batch{fileShareServiceConfig.Value.ProductType} businessUnit eq '{fileShareServiceConfig.Value.S63BusinessUnit}'";

            if (folderName == fileShareServiceConfig.Value.Info)
            {
                uri += $" and $batch(Content) eq '{fileShareServiceConfig.Value.ContentInfo}'";
            }
            else
            {
                uri += $" and $batch(Content) eq '{fileShareServiceConfig.Value.Content}'";
                uri += $" and $batch(Catalogue Type) eq '{fileShareServiceConfig.Value.Adc}'";
            }
            return await fileShareSearchService.SearchFolderDetails(batchId, correlationId, uri);
        }

        public async Task<bool> DownloadFolderDetails(string batchId, string correlationId, IEnumerable<BatchFile> fileDetails, string exchangeSetPath)
        {
            return await fileShareDownloadService.DownloadFolderDetails(batchId, correlationId, fileDetails, exchangeSetPath);
        }

        public async Task<bool> CommitExchangeSet(string batchId, string correlationId, string exchangeSetZipPath)
        {
            return await fileShareBatchService.CommitBatchToFss(batchId, correlationId, exchangeSetZipPath);
        }

        public async Task<bool> DownloadReadMeFileFromCacheAsync(string batchId, string exchangeSetRootPath, string correlationId)
        {
            return await fileShareDownloadService.DownloadReadMeFileFromCacheAsync(batchId,exchangeSetRootPath,correlationId);
        }
    }
}
