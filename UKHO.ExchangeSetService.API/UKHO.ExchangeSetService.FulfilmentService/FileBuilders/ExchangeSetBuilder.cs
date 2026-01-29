using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
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
                        logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation is cancelled as IsCancellationRequested flag is true in QueryFileShareServiceFiles with {Token} and batchId:{BatchId} and CorrelationId:{CorrelationId}", JsonConvert.SerializeObject(cancellationTokenSource.Token), batch.BatchId, batch.CorrelationId);
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
                        logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation is cancelled as IsCancellationRequested flag is true in QueryFileShareServiceFiles with {Token} and batchId:{BatchId} and CorrelationId:{CorrelationId}", JsonConvert.SerializeObject(cancellationTokenSource.Token), message.BatchId, message.CorrelationId);
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

        public async Task<bool> CreateStandardLargeMediaExchangeSet(FulfilmentServiceBatch batch, LargeExchangeSetDataResponse largeExchangeSetDataResponse, string largeExchangeSetFolderName, string largeMediaExchangeSetFilePath,
            CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            LargeExchangeSetDataResponse response = await SearchAndDownloadEncFilesFromFss(batch, largeExchangeSetFolderName, largeExchangeSetDataResponse, cancellationTokenSource, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrWhiteSpace(response.ValidationtFailedMessage))
            {
                logger.LogError(EventIds.LargeExchangeSetCreatedWithError.ToEventId(), "Large media exchange set is not created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batch.BatchId, batch.CorrelationId);
                logger.LogError(EventIds.LargeExchangeSetCreatedWithError.ToEventId(), "Operation Cancelled as product validation failed for BatchId:{BatchId}, _X-Correlation-ID:{CorrelationId} and Validation message :{Message}", batch.BatchId, batch.CorrelationId, response.ValidationtFailedMessage);
                throw new FulfilmentException(EventIds.BundleInfoValidationFailed.ToEventId());
            }

            var rootDirectories = fileSystemHelper.GetDirectoryInfo(batch.BatchDirectory)
                                                  .Where(di => di.Name.StartsWith("M0"));

            // Build each volume sequentially in definition but concurrently in execution.
            var volumeTasks = rootDirectories.Select(rd =>
                BuildLargeMediaVolumeAsync(batch.Message, rd, response,
                    largeExchangeSetFolderName, largeMediaExchangeSetFilePath, cancellationToken));

            var results = await Task.WhenAll(volumeTasks);
            return results.All(r => r);
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

        // Build a single large media volume:
        // - Validate cancellation
        // - Derive volume path and DVD number
        // - Pre-compute INFO and ADC paths
        // - Run ancillary tasks concurrently for the volume:
        //     - Create POS folder structure
        //     - Create media file
        //     - Download README
        //     - Create serial ENC file for large media
        //     - Create Product.txt in INFO (encryption=true)
        //     - Download INFO folder files
        //     - Download ADC folder files
        //     - Create ENC_UPDATE.CSV
        // - Await all tasks
        // - Validate cancellation
        // - Create catalog file for the volume (returns bool)
        private async Task<bool> BuildLargeMediaVolumeAsync(SalesCatalogueServiceResponseQueueMessage message, IFileSystemInfo rootDirectoryFolder, LargeExchangeSetDataResponse response,
                    string largeExchangeSetFolderName, string largeMediaExchangeSetFilePath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Null-safety and early exits to avoid throwing later
            if (message is null || response is null || rootDirectoryFolder is null)
                return false;

            var volumePath = rootDirectoryFolder.ToString();
            var dvdNumber = ParseDvdNumber(rootDirectoryFolder.Name);

            // Run ancillary tasks concurrently per volume.
            var tasks = new List<Task>
            {
                CreatePosFolderStructure(volumePath),
                fulfilmentAncillaryFiles.CreateMediaFile(message.BatchId, volumePath, message.CorrelationId, dvdNumber),
                download.DownloadLargeMediaReadMeFile(message.BatchId, volumePath, message.CorrelationId),
                fileBuilder.CreateLargeMediaSerialEncFile(message.BatchId, largeMediaExchangeSetFilePath,
                    string.Format(largeExchangeSetFolderName, dvdNumber), message.CorrelationId),
                fileBuilder.CreateProductFile(message.BatchId,
                    Path.Combine(volumePath, fileShareServiceConfig.Value.Info),
                    message.CorrelationId,
                    response.SalesCatalogueDataResponse,
                    message.ScsRequestDateTime,
                    true), // encryption=true
                download.DownloadInfoFolderFiles(message.BatchId,
                    Path.Combine(volumePath, fileShareServiceConfig.Value.Info),
                    message.CorrelationId),
                download.DownloadAdcFolderFiles(message.BatchId,
                    Path.Combine(volumePath, fileShareServiceConfig.Value.Info, fileShareServiceConfig.Value.Adc),
                    message.CorrelationId),
                fulfilmentAncillaryFiles.CreateEncUpdateCsv(response.SalesCatalogueDataResponse,
                    Path.Combine(volumePath, fileShareServiceConfig.Value.Info),
                    message.BatchId,
                    message.CorrelationId)
            };

            await Task.WhenAll(tasks);

            cancellationToken.ThrowIfCancellationRequested();

            // Catalog file (returns bool)
            bool catalogCreated = await fileBuilder.CreateLargeMediaExchangesetCatalogFile(
                message.BatchId,
                volumePath,
                message.CorrelationId,
                response.FulfilmentDataResponses,
                response.SalesCatalogueDataResponse,
                response.SalesCatalogueProductResponse);

            return catalogCreated;
        }
        private static string ParseDvdNumber(string directoryName)
        {
            return directoryName[^4..].Remove(1, 3);
        }

        //Search and download ENC files for large media exchange set
        private async Task<LargeExchangeSetDataResponse> SearchAndDownloadEncFilesFromFss(FulfilmentServiceBatch batch, string largeExchangeSetFolderName, LargeExchangeSetDataResponse largeExchangeSetDataResponse,
            CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batchPath = Path.Combine(batch.BatchDirectory, largeExchangeSetFolderName);
            var exchangeSetRootPath = Path.Combine(batchPath, "{1}", fileShareServiceConfig.Value.EncRoot);
            var listFulfilmentData = new List<FulfilmentDataResponse>();

            var businessUnit = batch.Message.ExchangeSetStandard.GetBusinessUnit(fileShareServiceConfig.Value);

            List<string> aioCells = !string.IsNullOrEmpty(aioConfiguration.Value.AioCells) ? new(aioConfiguration.Value.AioCells.Split(',')) : new List<string>();

            var essItems = largeExchangeSetDataResponse.SalesCatalogueProductResponse.Products
                .Where(product => aioCells.All(aioCell => product.ProductName != aioCell))
                .ToList();

            cancellationToken.ThrowIfCancellationRequested();

            var validationResult = await productDataValidator.Validate(essItems);

            if (!validationResult.IsValid)
            {
                largeExchangeSetDataResponse.ValidationtFailedMessage = validationResult.Errors[0].ToString();
                return largeExchangeSetDataResponse;
            }

            if (!essItems.Any())
                return largeExchangeSetDataResponse;

            DateTime started = DateTime.UtcNow;

            int parallelism = fileShareServiceConfig.Value.ParallelSearchTaskCount <= 0
                ? 4
                : fileShareServiceConfig.Value.ParallelSearchTaskCount;

            int productGroupCount = essItems.Count % parallelism == 0
                ? essItems.Count / parallelism
                : (essItems.Count / parallelism) + 1;

            var chunks = CommonHelper.SplitList(essItems, productGroupCount);
            var bag = new ConcurrentBag<FulfilmentDataResponse>();
            int totalQueries = 0;

            using var throttler = new SemaphoreSlim(parallelism);

            var tasks = chunks.Select(async chunk =>
            {
                await throttler.WaitAsync(cancellationToken);
                try
                {
                    var result = await QueryFileShareServiceFiles(
                        batch.Message,
                        chunk,
                        exchangeSetRootPath,
                        businessUnit,
                        cancellationTokenSource, cancellationToken);

                    if (result.Any())
                    {
                        Interlocked.Add(ref totalQueries, result.First().FileShareServiceSearchQueryCount);
                        foreach (var r in result)
                            bag.Add(r);
                    }
                }
                finally
                {
                    throttler.Release();
                }
            }).ToList();

            await Task.WhenAll(tasks);

            cancellationToken.ThrowIfCancellationRequested();

            int downloadedCount = bag.Sum(r => r.FileUri?.Count() ?? 0);
            monitorHelper.MonitorRequest("Query and Download ENC Files Task",
                started,
                DateTime.UtcNow,
                batch.Message.CorrelationId,
                totalQueries,
                downloadedCount,
                null,
                batch.BatchId);

            largeExchangeSetDataResponse.FulfilmentDataResponses = bag.ToList();
            return largeExchangeSetDataResponse;
        }

        public async Task<List<FulfilmentDataResponse>> QueryFileShareServiceFiles(
            SalesCatalogueServiceResponseQueueMessage message,
            List<Products> products,
            string exchangeSetRootPath,
            string businessUnit,
            CancellationTokenSource cancellationTokenSource,
            CancellationToken cancellationToken)
        {
            return await logger.LogStartEndAndElapsedTimeAsync(
                EventIds.QueryFileShareServiceENCFilesRequestStart,
                EventIds.QueryFileShareServiceENCFilesRequestCompleted,
                "File share service search & download for ENC files BusinessUnit:{businessUnit} BatchId:{BatchId} _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    return await fulfilmentFileShareService.QueryFileShareServiceData(
                        products,
                        message,
                        cancellationTokenSource: cancellationTokenSource,          // no longer passing internal CTS
                        cancellationToken: cancellationToken,
                        exchangeSetRootPath,
                        businessUnit);
                },
                businessUnit,
                message.BatchId,
                message.CorrelationId);
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
