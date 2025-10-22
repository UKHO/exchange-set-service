using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Models.WebJobs;
using UKHO.ExchangeSetService.FulfilmentService.Downloads;
using UKHO.ExchangeSetService.FulfilmentService.Services;
using UKHO.ExchangeSetService.FulfilmentService.Validation;

namespace UKHO.ExchangeSetService.FulfilmentService.FileBuilders
{
    public class ExchangeSetBuilder(ILogger<FulfilmentDataService> logger,
        IMonitorHelper monitorHelper,
        IFileSystemHelper fileSystemHelper,
        IProductDataValidator productDataValidator,
        IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
        IOptions<AioConfiguration> aioConfiguration,
        IFulfilmentFileShareService fulfilmentFileShareService,
        IFulfilmentAncillaryFiles fulfilmentAncillaryFiles,
        IFileBuilder fileBuilder,
        IDownloader download) : IExchangeSetBuilder
    {

        #region AIO Exchanges Set

        public async Task<bool> CreateAioExchangeSet(FulfilmentServiceBatch batch, List<Products> aioItems, SalesCatalogueDataResponse salesCatalogueEssDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse)
        {
            var aioExchangeSetPath = Path.Combine(batch.BatchDirectory, fileShareServiceConfig.Value.AioExchangeSetFileFolder);
            var aioExchangeSetRootPath = Path.Combine(aioExchangeSetPath, fileShareServiceConfig.Value.EncRoot);
            var listFulfilmentAioData = new List<FulfilmentDataResponse>();

            if (aioItems != null && aioItems.Any())
            {
                DateTime queryAndDownloadEncFilesFromFileShareServiceTaskStartedAt = DateTime.UtcNow;
                int parallelSearchTaskCount = fileShareServiceConfig.Value.ParallelSearchTaskCount;
                int productGroupCount = aioItems.Count % parallelSearchTaskCount == 0 ? aioItems.Count / parallelSearchTaskCount : (aioItems.Count / parallelSearchTaskCount) + 1;
                var productsList = CommonHelper.SplitList(aioItems, productGroupCount);
                var fulfilmentDataResponse = new List<FulfilmentDataResponse>();
                var sync = new object();
                int fileShareServiceSearchQueryCount = 0;
                var cancellationTokenSource = new CancellationTokenSource();
                CancellationToken cancellationToken = cancellationTokenSource.Token;

                var tasks = productsList.Select(async item =>
                {
                    //Only S63 data will be fetched
                    fulfilmentDataResponse = await QueryFileShareServiceFiles(batch.Message, item, aioExchangeSetRootPath, cancellationTokenSource, cancellationToken, fileShareServiceConfig.Value.S63BusinessUnit);
                    int queryCount = fulfilmentDataResponse.Any() ? fulfilmentDataResponse.First().FileShareServiceSearchQueryCount : 0;
                    lock (sync)
                    {
                        fileShareServiceSearchQueryCount += queryCount;
                    }
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancellationTokenSource.Cancel();
                        logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation is cancelled as IsCancellationRequested flag is true in QueryFileShareServiceFiles with {cancellationTokenSource.Token} and batchId:{message.BatchId} and CorrelationId:{message.CorrelationId}", JsonConvert.SerializeObject(cancellationTokenSource.Token), batch.BatchId, batch.CorrelationId);
                        throw new OperationCanceledException();
                    }
                    listFulfilmentAioData.AddRange(fulfilmentDataResponse);
                });

                await Task.WhenAll(tasks);

                DateTime queryAndDownloadEncFilesFromFileShareServiceTaskCompletedAt = DateTime.UtcNow;
                int downloadedENCFileCount = 0;
                foreach (var item in listFulfilmentAioData)
                {
                    downloadedENCFileCount += item.Files.Count();
                }
                monitorHelper.MonitorRequest("Query and Download ENC Files Task", queryAndDownloadEncFilesFromFileShareServiceTaskStartedAt, queryAndDownloadEncFilesFromFileShareServiceTaskCompletedAt, batch.CorrelationId, fileShareServiceSearchQueryCount, downloadedENCFileCount, null, batch.BatchId);
            }

            return await fileBuilder.CreateAncillaryFilesForAio(batch.BatchId, aioExchangeSetPath, batch.CorrelationId, salesCatalogueEssDataResponse, batch.Message.ScsRequestDateTime, salesCatalogueProductResponse, listFulfilmentAioData);
        }

        #endregion

        public async Task CreateStandardExchangeSet(SalesCatalogueServiceResponseQueueMessage message, SalesCatalogueProductResponse response, List<Products> essItems, string exchangeSetPath, SalesCatalogueDataResponse salesCatalogueEssDataResponse, string businessUnit)
        {
            var exchangeSetRootPath = Path.Combine(exchangeSetPath, fileShareServiceConfig.Value.EncRoot);
            var listFulfilmentData = new List<FulfilmentDataResponse>();

            if (essItems != null && essItems.Any())
            {
                DateTime queryAndDownloadEncFilesFromFileShareServiceTaskStartedAt = DateTime.UtcNow;
                int parallelSearchTaskCount = fileShareServiceConfig.Value.ParallelSearchTaskCount;
                int productGroupCount = essItems.Count % parallelSearchTaskCount == 0 ? essItems.Count / parallelSearchTaskCount : (essItems.Count / parallelSearchTaskCount) + 1;
                var productsList = CommonHelper.SplitList(essItems, productGroupCount);
                var fulfilmentDataResponse = new List<FulfilmentDataResponse>();
                var sync = new object();
                int fileShareServiceSearchQueryCount = 0;
                var cancellationTokenSource = new CancellationTokenSource();
                CancellationToken cancellationToken = cancellationTokenSource.Token;

                var tasks = productsList.Select(async item =>
                {
                    //S63 or S57 data will be fetched
                    fulfilmentDataResponse = await QueryFileShareServiceFiles(message, item, exchangeSetRootPath, cancellationTokenSource, cancellationToken, businessUnit);
                    int queryCount = fulfilmentDataResponse.Any() ? fulfilmentDataResponse.First().FileShareServiceSearchQueryCount : 0;
                    lock (sync)
                    {
                        fileShareServiceSearchQueryCount += queryCount;
                    }
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancellationTokenSource.Cancel();
                        logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation is cancelled as IsCancellationRequested flag is true in QueryFileShareServiceFiles with {cancellationTokenSource.Token} and batchId:{message.BatchId} and CorrelationId:{message.CorrelationId}", JsonConvert.SerializeObject(cancellationTokenSource.Token), message.BatchId, message.CorrelationId);
                        throw new OperationCanceledException();
                    }
                    listFulfilmentData.AddRange(fulfilmentDataResponse);
                });

                await Task.WhenAll(tasks);

                DateTime queryAndDownloadEncFilesFromFileShareServiceTaskCompletedAt = DateTime.UtcNow;
                int downloadedENCFileCount = 0;
                foreach (var item in listFulfilmentData)
                {
                    downloadedENCFileCount += item.Files.Count();
                }
                monitorHelper.MonitorRequest("Query and Download ENC Files Task", queryAndDownloadEncFilesFromFileShareServiceTaskStartedAt, queryAndDownloadEncFilesFromFileShareServiceTaskCompletedAt, message.CorrelationId, fileShareServiceSearchQueryCount, downloadedENCFileCount, null, message.BatchId);
            }

            bool encryption = string.Equals(businessUnit, fileShareServiceConfig.Value.S63BusinessUnit, StringComparison.OrdinalIgnoreCase);

            await fileBuilder.CreateAncillaryFiles(message.BatchId, exchangeSetPath, message.CorrelationId, listFulfilmentData, response, message.ScsRequestDateTime, salesCatalogueEssDataResponse, encryption);
        }

        public async Task<bool> CreateStandardLargeMediaExchangeSet(FulfilmentServiceBatch batch, LargeExchangeSetDataResponse largeExchangeSetDataResponse, string largeExchangeSetFolderName, string largeMediaExchangeSetFilePath)
        {
            LargeExchangeSetDataResponse response = await SearchAndDownloadEncFilesFromFss(batch, largeExchangeSetFolderName, largeExchangeSetDataResponse);
            if (!string.IsNullOrWhiteSpace(response.ValidationtFailedMessage))
            {
                logger.LogError(EventIds.LargeExchangeSetCreatedWithError.ToEventId(), "Large media exchange set is not created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batch.BatchId, batch.CorrelationId);
                logger.LogError(EventIds.LargeExchangeSetCreatedWithError.ToEventId(), "Operation Cancelled as product validation failed for BatchId:{BatchId}, _X-Correlation-ID:{CorrelationId} and Validation message :{Message}", batch.BatchId, batch.CorrelationId, response.ValidationtFailedMessage);
                throw new FulfilmentException(EventIds.BundleInfoValidationFailed.ToEventId());
            }

            var rootDirectories = fileSystemHelper.GetDirectoryInfo(batch.BatchDirectory)
                                                  .Where(di => di.Name.StartsWith("M0"));

            var ParallelCreateFolderTasks = new List<Task> { };
            Parallel.ForEach(rootDirectories, rootDirectoryFolder =>
            {
                string dvdNumber = rootDirectoryFolder.ToString()[^4..].Remove(1, 3);

                ParallelCreateFolderTasks.Add(CreatePosFolderStructure(rootDirectoryFolder.ToString()));
                ParallelCreateFolderTasks.Add(fulfilmentAncillaryFiles.CreateMediaFile(batch.BatchId, rootDirectoryFolder.ToString(), batch.CorrelationId, dvdNumber.ToString()));
                ParallelCreateFolderTasks.Add(download.DownloadLargeMediaReadMeFile(batch.BatchId, rootDirectoryFolder.ToString(), batch.CorrelationId));
                ParallelCreateFolderTasks.Add(fileBuilder.CreateLargeMediaSerialEncFile(batch.BatchId, largeMediaExchangeSetFilePath, string.Format(largeExchangeSetFolderName, dvdNumber), batch.CorrelationId));
                ParallelCreateFolderTasks.Add(fileBuilder.CreateProductFile(batch.BatchId, Path.Combine(rootDirectoryFolder.ToString(), fileShareServiceConfig.Value.Info), batch.CorrelationId, response.SalesCatalogueDataResponse, batch.Message.ScsRequestDateTime, true)); //encryption=true since we will only request S63 large media exchange set.
                ParallelCreateFolderTasks.Add(download.DownloadInfoFolderFiles(batch.BatchId, Path.Combine(rootDirectoryFolder.ToString(), fileShareServiceConfig.Value.Info), batch.CorrelationId));
                ParallelCreateFolderTasks.Add(download.DownloadAdcFolderFiles(batch.BatchId, Path.Combine(rootDirectoryFolder.ToString(), fileShareServiceConfig.Value.Info, fileShareServiceConfig.Value.Adc), batch.CorrelationId));
                ParallelCreateFolderTasks.Add(fulfilmentAncillaryFiles.CreateEncUpdateCsv(response.SalesCatalogueDataResponse, Path.Combine(rootDirectoryFolder.ToString(), fileShareServiceConfig.Value.Info), batch.BatchId, batch.CorrelationId));
            });

            await Task.WhenAll(ParallelCreateFolderTasks);
            ParallelCreateFolderTasks.Clear();

            var ParallelCreateFolderTaskForCatlogFile = new List<Task<bool>> { };
            Parallel.ForEach(rootDirectories, rootDirectoryFolder =>
            {
                ParallelCreateFolderTaskForCatlogFile.Add(fileBuilder.CreateLargeMediaExchangesetCatalogFile(batch.BatchId, rootDirectoryFolder.ToString(), batch.CorrelationId, response.FulfilmentDataResponses, response.SalesCatalogueDataResponse, response.SalesCatalogueProductResponse));
            });

            await Task.WhenAll(ParallelCreateFolderTaskForCatlogFile);
            bool isExchangeSetFolderCreated = await Task.FromResult(ParallelCreateFolderTaskForCatlogFile.All(x => x.Result.Equals(true)));
            ParallelCreateFolderTaskForCatlogFile.Clear();

            return isExchangeSetFolderCreated;
        }
        private async Task CreatePosFolderStructure(string largeMediaExchangeSetPath)
        {
            fileSystemHelper.CheckAndCreateFolder(largeMediaExchangeSetPath);
            var largeMediaExchangeSetInfoPath = Path.Combine(largeMediaExchangeSetPath, "INFO");
            fileSystemHelper.CheckAndCreateFolder(largeMediaExchangeSetInfoPath);
            var largeMediaExchangeSetAdcPath = Path.Combine(largeMediaExchangeSetInfoPath, "ADC");
            fileSystemHelper.CheckAndCreateFolder(largeMediaExchangeSetAdcPath);
            await Task.CompletedTask;
        }

        //Search and download ENC files for large media exchange set
        private async Task<LargeExchangeSetDataResponse> SearchAndDownloadEncFilesFromFss(FulfilmentServiceBatch batch, string largeExchangeSetFolderName, LargeExchangeSetDataResponse largeExchangeSetDataResponse)
        {
            var batchPath = Path.Combine(batch.BatchDirectory, largeExchangeSetFolderName);
            var exchangeSetRootPath = Path.Combine(batchPath, "{1}", fileShareServiceConfig.Value.EncRoot);
            var listFulfilmentData = new List<FulfilmentDataResponse>();

            List<string> aioCells = !string.IsNullOrEmpty(aioConfiguration.Value.AioCells) ? new(aioConfiguration.Value.AioCells.Split(',')) : new List<string>();

            var essItems = largeExchangeSetDataResponse.SalesCatalogueProductResponse.Products
                .Where(product => aioCells.All(aioCell => product.ProductName != aioCell))
                .ToList();

            Task<ValidationResult> validationResult = productDataValidator.Validate(essItems);

            if (!validationResult.Result.IsValid)
            {
                largeExchangeSetDataResponse.ValidationtFailedMessage = validationResult.Result.Errors[0].ToString();
                return largeExchangeSetDataResponse;
            }

            if (essItems != null && essItems.Any())
            {
                DateTime queryAndDownloadEncFilesFromFileShareServiceTaskStartedAt = DateTime.UtcNow;
                int parallelSearchTaskCount = fileShareServiceConfig.Value.ParallelSearchTaskCount;
                int productGroupCount = essItems.Count % parallelSearchTaskCount == 0 ? essItems.Count / parallelSearchTaskCount : (essItems.Count / parallelSearchTaskCount) + 1;
                var productsList = CommonHelper.SplitList(essItems, productGroupCount);
                var fulfilmentDataResponse = new List<FulfilmentDataResponse>();
                var sync = new object();
                int fileShareServiceSearchQueryCount = 0;
                var cancellationTokenSource = new CancellationTokenSource();
                CancellationToken cancellationToken = cancellationTokenSource.Token;

                var tasks = productsList.Select(async item =>
                {
                    //Only S63 data will be fetched
                    fulfilmentDataResponse = await QueryFileShareServiceFiles(batch.Message, item, exchangeSetRootPath, cancellationTokenSource, cancellationToken, fileShareServiceConfig.Value.S63BusinessUnit);
                    int queryCount = fulfilmentDataResponse.Any() ? fulfilmentDataResponse.First().FileShareServiceSearchQueryCount : 0;
                    lock (sync)
                    {
                        fileShareServiceSearchQueryCount += queryCount;
                    }
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancellationTokenSource.Cancel();
                        logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation is cancelled as IsCancellationRequested flag is true in QueryFileShareServiceFiles with {cancellationTokenSource.Token} and batchId:{message.BatchId} and CorrelationId:{message.CorrelationId}", JsonConvert.SerializeObject(cancellationTokenSource.Token), batch.BatchId, batch.CorrelationId);
                        throw new OperationCanceledException();
                    }
                    listFulfilmentData.AddRange(fulfilmentDataResponse);
                });

                await Task.WhenAll(tasks);

                DateTime queryAndDownloadEncFilesFromFileShareServiceTaskCompletedAt = DateTime.UtcNow;
                int downloadedENCFileCount = 0;
                foreach (var item in listFulfilmentData)
                {
                    downloadedENCFileCount += item.Files.Count();
                }
                monitorHelper.MonitorRequest("Query and Download ENC Files Task", queryAndDownloadEncFilesFromFileShareServiceTaskStartedAt, queryAndDownloadEncFilesFromFileShareServiceTaskCompletedAt, batch.CorrelationId, fileShareServiceSearchQueryCount, downloadedENCFileCount, null, batch.BatchId);
                largeExchangeSetDataResponse.FulfilmentDataResponses = listFulfilmentData;
            }

            return largeExchangeSetDataResponse;
        }

        public async Task<List<FulfilmentDataResponse>> QueryFileShareServiceFiles(SalesCatalogueServiceResponseQueueMessage message, List<Products> products, string exchangeSetRootPath, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken, string businessUnit)
        {
            return await logger.LogStartEndAndElapsedTimeAsync(EventIds.QueryFileShareServiceENCFilesRequestStart,
                   EventIds.QueryFileShareServiceENCFilesRequestCompleted,
                   "File share service search query and download request for ENC files from BusinessUnit:{businessUnit} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                   async () =>
                   {
                       return await fulfilmentFileShareService.QueryFileShareServiceData(products, message, cancellationTokenSource, cancellationToken, exchangeSetRootPath, businessUnit);
                   },
                   businessUnit, message.BatchId, message.CorrelationId);
        }
    }
}
