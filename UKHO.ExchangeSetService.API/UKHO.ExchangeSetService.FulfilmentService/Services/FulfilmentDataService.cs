using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.FulfilmentService.FileBuilders;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public class FulfilmentDataService(IAzureBlobStorageService azureBlobStorageService,
                                IFulfilmentFileShareService fulfilmentFileShareService,
                                ILogger<FulfilmentDataService> logger,
                                IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
                                IConfiguration configuration,
                                IFulfilmentSalesCatalogueService fulfilmentSalesCatalogueService,
                                IFulfilmentCallBackService fulfilmentCallBackService,
                                IMonitorHelper monitorHelper,
                                IOptions<AioConfiguration> aioConfiguration,
                                IFileSystemHelper fileSystemHelper,
                                IExchangeSetBuilder exchangeSetBuilder) : IFulfilmentDataService
    {
        private readonly AioConfiguration aioConfiguration = aioConfiguration.Value;

        public async Task<string> CreateExchangeSet(SalesCatalogueServiceResponseQueueMessage message, string currentUtcDate)
        {
            DateTime createExchangeSetTaskStartedAt = DateTime.UtcNow;
            string homeDirectoryPath = configuration["HOME"];
            var exchangeSetPath = Path.Combine(homeDirectoryPath, currentUtcDate, message.BatchId, fileShareServiceConfig.Value.ExchangeSetFileFolder);
            var exchangeSetZipFilePath = Path.Combine(homeDirectoryPath, currentUtcDate, message.BatchId);

            //Get SCS catalogue essData response
            SalesCatalogueDataResponse salesCatalogueEssDataResponse = await GetSalesCatalogueDataResponse(message.BatchId, message.CorrelationId);

            var response = await DownloadSalesCatalogueResponse(message);

            List<string> aioCells = !string.IsNullOrEmpty(aioConfiguration.AioCells) ? new(aioConfiguration.AioCells.Split(',')) : new List<string>();
            var essItems = response.Products
                .Where(product => aioCells.All(aioCell => product.ProductName != aioCell))
                .ToList();

            var aioItems = response.Products
                    .Where(product => aioCells.Any(aioCell => product.ProductName == aioCell))
                    .ToList();

            var businessUnit = message.ExchangeSetStandard.GetBusinessUnit(fileShareServiceConfig.Value);

            SalesCatalogueDataResponse salesCatalogueEssDataResponseForAio = (SalesCatalogueDataResponse)salesCatalogueEssDataResponse.Clone();

            if (essItems != null && essItems.Any() || message.IsEmptyEncExchangeSet)
            {
                salesCatalogueEssDataResponse.ResponseBody = salesCatalogueEssDataResponse.ResponseBody
                                                             .Where(x => !aioCells.Any(productName => productName.Equals(x.ProductName))).ToList();
                await exchangeSetBuilder.CreateStandardExchangeSet(message, response, essItems, exchangeSetPath, salesCatalogueEssDataResponse, businessUnit);
            }
            if (aioItems != null && aioItems.Any() || message.IsEmptyAioExchangeSet)
            {
                salesCatalogueEssDataResponseForAio.ResponseBody = salesCatalogueEssDataResponseForAio.ResponseBody
                                                     .Where(x => aioCells.Any(productName => productName.Equals(x.ProductName))).ToList();
                await exchangeSetBuilder.CreateAioExchangeSet(message, currentUtcDate, homeDirectoryPath, aioItems, salesCatalogueEssDataResponseForAio, response);
            }


            bool isZipFileUploaded = await PackageAndUploadExchangeSetZipFileToFileShareService(message.BatchId, exchangeSetZipFilePath, message.CorrelationId);

            DateTime createExchangeSetTaskCompletedAt = DateTime.UtcNow;
            if (isZipFileUploaded)
            {
                logger.LogInformation(EventIds.ExchangeSetCreated.ToEventId(), "Exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", message.BatchId, message.CorrelationId);
                await fulfilmentCallBackService.SendCallBackResponse(response, message);
                monitorHelper.MonitorRequest("Create Exchange Set Task", createExchangeSetTaskStartedAt, createExchangeSetTaskCompletedAt, message.CorrelationId, null, null, null, message.BatchId);
                return "Exchange Set Created Successfully";
            }
            monitorHelper.MonitorRequest("Create Exchange Set Task", createExchangeSetTaskStartedAt, createExchangeSetTaskCompletedAt, message.CorrelationId, null, null, null, message.BatchId);
            return "Exchange Set Is Not Created";
        }

        public async Task<string> CreateLargeExchangeSet(SalesCatalogueServiceResponseQueueMessage message, string currentUtcDate, string largeExchangeSetFolderName)
        {
            DateTime createExchangeSetTaskStartedAt = DateTime.UtcNow;
            string homeDirectoryPath = configuration["HOME"];
            var exchangeSetFilePath = Path.Combine(homeDirectoryPath, currentUtcDate, message.BatchId);
            bool isExchangeSetFolderCreated = false;
            bool isZipAndUploadSuccessful = false;

            var response = new LargeExchangeSetDataResponse
            {
                //Get SCS catalogue essData response
                SalesCatalogueDataResponse = await GetSalesCatalogueDataResponse(message.BatchId, message.CorrelationId),
                SalesCatalogueProductResponse = await DownloadSalesCatalogueResponse(message)
            };

            List<string> aioCells = !string.IsNullOrEmpty(aioConfiguration.AioCells) ? new(aioConfiguration.AioCells.Split(',')) : new List<string>();
            var essItems = response.SalesCatalogueProductResponse.Products
                .Where(product => aioCells.All(aioCell => product.ProductName != aioCell))
                .ToList();

            var aioItems = response.SalesCatalogueProductResponse.Products
                    .Where(product => aioCells.Any(aioCell => product.ProductName == aioCell))
                    .ToList();

            var largeExchangeSetDataResponseForAio = new LargeExchangeSetDataResponse()
            {
                SalesCatalogueDataResponse = (SalesCatalogueDataResponse)response.SalesCatalogueDataResponse.Clone(),
                SalesCatalogueProductResponse = response.SalesCatalogueProductResponse
            };

            if (essItems.Count > 0)
            {
                response.SalesCatalogueDataResponse.ResponseBody = response.SalesCatalogueDataResponse.ResponseBody
                                                                   .Where(x => !aioCells.Any(productName => productName == x.ProductName)).ToList();
                isExchangeSetFolderCreated = await exchangeSetBuilder.CreateStandardLargeMediaExchangeSet(message, homeDirectoryPath, currentUtcDate, response, largeExchangeSetFolderName, exchangeSetFilePath);

                if (!isExchangeSetFolderCreated)
                {
                    logger.LogError(EventIds.LargeExchangeSetCreatedWithError.ToEventId(), "Large media exchange creation failed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", message.BatchId, message.CorrelationId);
                    throw new FulfilmentException(EventIds.LargeExchangeSetCreatedWithError.ToEventId());
                }
            }

            if (aioItems.Count > 0)
            {
                largeExchangeSetDataResponseForAio.SalesCatalogueDataResponse.ResponseBody = largeExchangeSetDataResponseForAio.SalesCatalogueDataResponse.ResponseBody
                                                                                    .Where(x => aioCells.Any(productName => productName == x.ProductName)).ToList();
                isExchangeSetFolderCreated = await exchangeSetBuilder.CreateAioExchangeSet(message, currentUtcDate, homeDirectoryPath, aioItems, largeExchangeSetDataResponseForAio.SalesCatalogueDataResponse, largeExchangeSetDataResponseForAio.SalesCatalogueProductResponse);

                if (!isExchangeSetFolderCreated)
                {
                    logger.LogError(EventIds.AIOExchangeSetCreatedWithError.ToEventId(), "AIO exchange creation failed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", message.BatchId, message.CorrelationId);
                    throw new FulfilmentException(EventIds.AIOExchangeSetCreatedWithError.ToEventId());
                }
            }

            if (isExchangeSetFolderCreated)
            {
                var rootDirectories = fileSystemHelper.GetDirectoryInfo(Path.Combine(homeDirectoryPath, currentUtcDate, message.BatchId));

                var parallelZipUploadTasks = new List<Task<bool>> { };
                Parallel.ForEach(rootDirectories, rootDirectoryFolder =>
                {
                    if (rootDirectoryFolder.Name.StartsWith("M0"))  //Large Media Exchange Set
                    {
                        string dvdNumber = rootDirectoryFolder.ToString()[^4..].Remove(1, 3);
                        parallelZipUploadTasks.Add(PackageAndUploadLargeMediaExchangeSetZipFileToFileShareService(message.BatchId, rootDirectoryFolder.ToString(), exchangeSetFilePath, message.CorrelationId, string.Format(largeExchangeSetFolderName, dvdNumber.ToString())));
                    }
                    else // AIO
                    {
                        parallelZipUploadTasks.Add(PackageAndUploadLargeMediaExchangeSetZipFileToFileShareService(message.BatchId, rootDirectoryFolder.ToString(), exchangeSetFilePath, message.CorrelationId, fileShareServiceConfig.Value.AioExchangeSetFileFolder));
                    }
                });

                await Task.WhenAll(parallelZipUploadTasks);
                isZipAndUploadSuccessful = await Task.FromResult(parallelZipUploadTasks.All(x => x.Result.Equals(true)));
                parallelZipUploadTasks.Clear();
            }

            if (isZipAndUploadSuccessful)
            {
                var isBatchCommitted = await fulfilmentFileShareService.CommitLargeMediaExchangeSet(message.BatchId, exchangeSetFilePath, message.CorrelationId);
                if (isBatchCommitted)
                {
                    logger.LogInformation(EventIds.ExchangeSetCreated.ToEventId(), "Large media exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", message.BatchId, message.CorrelationId);
                    monitorHelper.MonitorRequest("Create Exchange Set Task", createExchangeSetTaskStartedAt, DateTime.UtcNow, message.CorrelationId, null, null, null, message.BatchId);
                    return "Large Media Exchange Set Created Successfully";
                }
            }

            monitorHelper.MonitorRequest("Create Exchange Set Task", createExchangeSetTaskStartedAt, DateTime.UtcNow, message.CorrelationId, null, null, null, message.BatchId);
            return "Large Media Exchange Set Is Not Created";
        }

        private async Task<SalesCatalogueProductResponse> DownloadSalesCatalogueResponse(SalesCatalogueServiceResponseQueueMessage message)
        {
            return await azureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, message.BatchId, message.CorrelationId);
        }

        private async Task<SalesCatalogueDataResponse> GetSalesCatalogueDataResponse(string batchId, string correlationId)
        {
            SalesCatalogueDataResponse salesCatalogueTypeResponse = await fulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(batchId, correlationId);
            return salesCatalogueTypeResponse;
        }

        private async Task<bool> PackageAndUploadLargeMediaExchangeSetZipFileToFileShareService(string batchId, string exchangeSetPath, string exchangeSetZipFilePath, string correlationId, string mediaZipFileName)
        {
            bool isZipFileUploaded = false;
            bool isZipFileCreated = false;
            DateTime createZipFileTaskStartedAt = DateTime.UtcNow;

            isZipFileCreated = await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateZipFileRequestStart,
                       EventIds.CreateZipFileRequestCompleted,
                       "Create large media exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                       async () => await fulfilmentFileShareService.CreateZipFileForExchangeSet(batchId, exchangeSetPath, correlationId),
                       batchId, correlationId);

            monitorHelper.MonitorRequest("Create Zip File Task", createZipFileTaskStartedAt, DateTime.UtcNow, correlationId, null, null, null, batchId);

            if (isZipFileCreated)
            {
                isZipFileUploaded = await logger.LogStartEndAndElapsedTimeAsync(EventIds.UploadExchangeSetToFssStart,
                      EventIds.UploadExchangeSetToFssCompleted,
                      "Upload large media exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                      async () => await fulfilmentFileShareService.UploadZipFileForLargeMediaExchangeSetToFileShareService(batchId, exchangeSetZipFilePath, correlationId, $"{mediaZipFileName}.zip"),
                      batchId, correlationId);
            }
            return isZipFileUploaded;
        }

        private async Task<bool> PackageAndUploadExchangeSetZipFileToFileShareService(string batchId, string exchangeSetZipFilePath, string correlationId)
        {
            bool isZipFileCreated = false;
            bool isZipFileUploaded = false;
            bool isBatchCommitted = false;

            IDirectoryInfo[] dir = fileSystemHelper.GetSubDirectories(exchangeSetZipFilePath);
            DateTime createZipFileTaskStartedAt = DateTime.UtcNow;

            foreach (var dirPath in dir)
            {
                isZipFileCreated = await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateZipFileRequestStart,
                       EventIds.CreateZipFileRequestCompleted,
                       "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                       async () =>
                       {
                           return await fulfilmentFileShareService.CreateZipFileForExchangeSet(batchId, dirPath.FullName, correlationId);
                       },
                       batchId, correlationId);

                if (!isZipFileCreated)
                {
                    logger.LogError(EventIds.ErrorInCreatingZipFile.ToEventId(), "Error in creating exchange set zip:{ExchangeSetFileName} for BatchId:{BatchId} and _X-Correlation-ID:{correlationId}", dirPath.Name + ".zip", batchId, correlationId);
                    throw new FulfilmentException(EventIds.ErrorInCreatingZipFile.ToEventId());
                }
            }
            DateTime createZipFileTaskCompletedAt = DateTime.UtcNow;
            monitorHelper.MonitorRequest("Create Zip File Task", createZipFileTaskStartedAt, createZipFileTaskCompletedAt, correlationId, null, null, null, batchId);

            if (isZipFileCreated)
            {
                IFileInfo[] fileInfos = fileSystemHelper.GetZipFiles(exchangeSetZipFilePath);

                foreach (var file in fileInfos)
                {
                    isZipFileUploaded = await logger.LogStartEndAndElapsedTimeAsync(EventIds.UploadExchangeSetToFssStart,
                      EventIds.UploadExchangeSetToFssCompleted,
                      "Upload exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                      async () =>
                      {
                          return await fulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(batchId, exchangeSetZipFilePath, correlationId, file.Name);
                      },
                    batchId, correlationId);
                }
            }

            if (isZipFileUploaded)
            {
                isBatchCommitted = await fulfilmentFileShareService.CommitExchangeSet(batchId, correlationId, exchangeSetZipFilePath);
            }

            return isBatchCommitted;
        }
    }
}
