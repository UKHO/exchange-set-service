using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.FulfilmentService.Downloads;
using UKHO.ExchangeSetService.FulfilmentService.Services;

namespace UKHO.ExchangeSetService.FulfilmentService.FileBuilders
{
    public class FileBuilder(
        ILogger<FulfilmentDataService> logger,
        IMonitorHelper monitorHelper,
        IFileSystemHelper fileSystemHelper,
        IDownloader download,
        IFulfilmentAncillaryFiles fulfilmentAncillaryFiles,
        IOptions<FileShareServiceConfiguration> fileShareServiceConfig) : IFileBuilder
    {
        public async Task CreateAncillaryFiles(BatchInfo batchInfo, IEnumerable<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueProductResponse salecatalogueProductResponse, DateTime scsRequestDateTime, SalesCatalogueDataResponse salesCatalogueEssDataResponse, bool encryption)
        {
            var exchangeSetRootPath = Path.Combine(batchInfo.Path, fileShareServiceConfig.Value.EncRoot);
            var exchangeSetInfoPath = Path.Combine(batchInfo.Path, fileShareServiceConfig.Value.Info);

            await CreateProductFile(batchInfo, salesCatalogueEssDataResponse, scsRequestDateTime, encryption);
            await CreateSerialEncFile(batchInfo);
            await download.DownloadReadMeFileAsync(batchInfo);
            await CreateCatalogFile(batchInfo, listFulfilmentData, salesCatalogueEssDataResponse, salecatalogueProductResponse);
        }

        public async Task<bool> CreateAncillaryFilesForAio(BatchInfo batchInfo, SalesCatalogueDataResponse salesCatalogueDataResponse, DateTime scsRequestDateTime, SalesCatalogueProductResponse salesCatalogueProductResponse, IEnumerable<FulfilmentDataResponse> listFulfilmentAioData)
        {
            var exchangeSetRootPath = Path.Combine(batchInfo.Path, fileShareServiceConfig.Value.EncRoot);
            var exchangeSetInfoPath = Path.Combine(batchInfo.Path, fileShareServiceConfig.Value.Info);

            using CancellationTokenSource cts = new();

            var tasks = new List<Task<bool>>()
            {
                download.DownloadReadMeFileAsync(batchInfo),
                         download.DownloadIhoCrtFile(batchInfo),
                         download.DownloadIhoPubFile(batchInfo),
                         CreateSerialAioFile(batchInfo, salesCatalogueDataResponse),
                         CreateProductFileForAio(new BatchInfo(batchInfo.BatchId, exchangeSetInfoPath, batchInfo.CorrelationId), salesCatalogueDataResponse, scsRequestDateTime),
                         CreateCatalogFileForAio(new BatchInfo(batchInfo.BatchId, exchangeSetRootPath, batchInfo.CorrelationId), listFulfilmentAioData, salesCatalogueDataResponse, salesCatalogueProductResponse)
        };

            // Run all tasks in parallel, if one fails stop and return a false
            Task<bool>[] enhancedTasks = [.. tasks.Select(async task =>
            {
                try
                {
                    bool result = await task.ConfigureAwait(false);
                    if (!result) cts.Cancel();
                    return result;
                }
                catch (OperationCanceledException) when (cts.IsCancellationRequested)
                {
                    return false;
                }
            })];

            bool[] results = await Task.WhenAll(enhancedTasks).ConfigureAwait(false);
            return results.All(x => x);
        }

        public async Task<bool> CreateSerialAioFile(BatchInfo batchInfo, SalesCatalogueDataResponse salesCatalogueDataResponse)
        {
            bool isSerialAioCreated = await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateSerialAioFileRequestStart,
                      EventIds.CreateSerialAioFileRequestCompleted,
                      "Create serial aio file request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                      async () =>
                      {
                          return await fulfilmentAncillaryFiles.CreateSerialAioFile(batchInfo, salesCatalogueDataResponse);
                      },
                  batchInfo.BatchId, batchInfo.CorrelationId);

            return isSerialAioCreated;
        }

        public async Task<bool> CreateProductFileForAio(BatchInfo batchInfo, SalesCatalogueDataResponse salesCatalogueDataResponse, DateTime scsRequestDateTime)
        {
            bool isProductFileCreated = false;

            if (!string.IsNullOrWhiteSpace(batchInfo.Path))
            {
                DateTime createProductFileTaskStartedAt = DateTime.UtcNow;
                isProductFileCreated = await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateProductFileRequestForAioStart,
                        EventIds.CreateProductFileRequestForAioCompleted,
                        "Create aio exchange set product file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                        async () =>
                        {
                            return await fulfilmentAncillaryFiles.CreateProductFile(batchInfo, salesCatalogueDataResponse, scsRequestDateTime);
                        },
                        batchInfo.BatchId, batchInfo.CorrelationId);

                DateTime createProductFileTaskCompletedAt = DateTime.UtcNow;
                monitorHelper.MonitorRequest("Create Product File Task", createProductFileTaskStartedAt, createProductFileTaskCompletedAt, batchInfo.CorrelationId, null, null, null, batchInfo.BatchId);
            }

            return isProductFileCreated;
        }

        public async Task<bool> CreateCatalogFileForAio(BatchInfo batchInfo, IEnumerable<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse)
        {
            bool isFileCreated = false;

            if (!string.IsNullOrWhiteSpace(batchInfo.Path))
            {
                DateTime createCatalogFileForAioTaskStartedAt = DateTime.UtcNow;
                isFileCreated = await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateCatalogFileForAioRequestStart,
                        EventIds.CreateCatalogFileForAioRequestCompleted,
                        "Create AIO exchange set catalog file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                        async () =>
                        {
                            return await fulfilmentAncillaryFiles.CreateCatalogFile(batchInfo, listFulfilmentData, salesCatalogueDataResponse, salesCatalogueProductResponse);
                        },
                        batchInfo.BatchId, batchInfo.CorrelationId);

                DateTime createCatalogFileForAioTaskCompletedAt = DateTime.UtcNow;
                monitorHelper.MonitorRequest("Create Catalog File Task", createCatalogFileForAioTaskStartedAt, createCatalogFileForAioTaskCompletedAt, batchInfo.CorrelationId, null, null, null, batchInfo.BatchId);
            }

            return isFileCreated;
        }

        public async Task<bool> CreateLargeMediaSerialEncFile(BatchInfo batchInfo, string rootfolder)
        {
            DateTime createLargeMediaSerialEncFileTaskStartedAt = DateTime.UtcNow;

            return await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateSerialFileRequestStart,
                      EventIds.CreateSerialFileRequestCompleted,
                      "Create large media serial enc file request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                      async () =>
                      {
                          var rootLastDirectoryPath = fileSystemHelper.GetDirectoryInfo(batchInfo.Path)
                                                  .LastOrDefault(di => di.Name.StartsWith("M0"));

                          var baseDirectoryies = fileSystemHelper.GetDirectoryInfo(Path.Combine(batchInfo.Path, rootfolder))
                                                  .Where(di => di.Name.StartsWith("B") && di.Name.Length <= 3 && CommonHelper.IsNumeric(di.Name[^(di.Name.Length - 1)..]));

                          var baseLastDirectory = fileSystemHelper.GetDirectoryInfo(rootLastDirectoryPath?.ToString())
                                                  .LastOrDefault(di => di.Name.StartsWith("B") && di.Name.Length <= 3 && CommonHelper.IsNumeric(di.Name[^(di.Name.Length - 1)..]));

                          string lastBaseDirectoryNumber = baseLastDirectory.ToString().Replace(Path.Combine(rootLastDirectoryPath.ToString(), "B"), "");

                          var ParallelBaseFolderTasks = new List<Task<bool>> { };
                          Parallel.ForEach(baseDirectoryies, baseDirectoryFolder =>
                          {
                              string baseDirectoryNumber = baseDirectoryFolder.ToString().Replace(Path.Combine(batchInfo.Path, rootfolder, "B"), "");
                              ParallelBaseFolderTasks.Add(fulfilmentAncillaryFiles.CreateLargeMediaSerialEncFile(new BatchInfo(batchInfo.BatchId, baseDirectoryFolder.ToString(), batchInfo.CorrelationId), baseDirectoryNumber, lastBaseDirectoryNumber));
                          });
                          await Task.WhenAll(ParallelBaseFolderTasks);

                          DateTime createLargeMediaSerialEncFileTaskCompletedAt = DateTime.UtcNow;
                          monitorHelper.MonitorRequest("Create Large Media Serial Enc File Task", createLargeMediaSerialEncFileTaskStartedAt, createLargeMediaSerialEncFileTaskCompletedAt, batchInfo.CorrelationId, null, null, null, batchInfo.BatchId);

                          return await Task.FromResult(ParallelBaseFolderTasks.All(x => x.Result.Equals(true)));
                      },
                  batchInfo.BatchId, batchInfo.CorrelationId);
        }

        public async Task<bool> CreateLargeMediaExchangesetCatalogFile(BatchInfo batchInfo, IEnumerable<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse)
        {
            var baseDirectory = fileSystemHelper.GetDirectoryInfo(batchInfo.Path)
                       .Where(di => di.Name.StartsWith("B") && di.Name.Length <= 3 && CommonHelper.IsNumeric(di.Name[^(di.Name.Length - 1)..]));

            var encFolderList = new List<string>();
            foreach (var directory in baseDirectory)
            {
                var encFolder = Path.Combine(directory.ToString(), fileShareServiceConfig.Value.EncRoot);
                encFolderList.Add(encFolder);
            }
            var ParallelCreateFolderTasks = new List<Task<bool>> { };

            Parallel.ForEach(encFolderList, encFolder =>
            {
                var countryCodes = fileSystemHelper.GetDirectoryInfo(encFolder)
                                   .Select(di => di.Name[^2..]).ToList();

                var fulfilmentDataBasedonCountryCode = listFulfilmentData.Where(x => countryCodes.Any(z => x.ProductName.StartsWith(z))).ToList();
                ParallelCreateFolderTasks.Add(fulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(new BatchInfo(batchInfo.BatchId, encFolder, batchInfo.CorrelationId), fulfilmentDataBasedonCountryCode, salesCatalogueDataResponse, salesCatalogueProductResponse));
            });
            await Task.WhenAll(ParallelCreateFolderTasks);
            var isCreateFolderTasksSuccessful = await Task.FromResult(ParallelCreateFolderTasks.All(x => x.Result.Equals(true)));
            ParallelCreateFolderTasks.Clear();

            return isCreateFolderTasksSuccessful;
        }

        public async Task<bool> CreateCatalogFile(BatchInfo batchInfo, IEnumerable<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse)
        {
            bool isFileCreated = false;

            if (!string.IsNullOrWhiteSpace(batchInfo.Path))
            {
                DateTime createCatalogFileTaskStartedAt = DateTime.UtcNow;
                isFileCreated = await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateCatalogFileRequestStart,
                        EventIds.CreateCatalogFileRequestCompleted,
                        "Create catalog file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                        async () =>
                        {
                            return await fulfilmentAncillaryFiles.CreateCatalogFile(batchInfo, listFulfilmentData, salesCatalogueDataResponse, salesCatalogueProductResponse);
                        },
                        batchInfo.BatchId, batchInfo.CorrelationId);

                DateTime createCatalogFileTaskCompletedAt = DateTime.UtcNow;
                monitorHelper.MonitorRequest("Create Catalog File Task", createCatalogFileTaskStartedAt, createCatalogFileTaskCompletedAt, batchInfo.CorrelationId, null, null, null, batchInfo.BatchId);
            }

            return isFileCreated;
        }

        public async Task<bool> CreateProductFile(BatchInfo batchInfo, SalesCatalogueDataResponse salesCatalogueDataResponse, DateTime scsRequestDateTime, bool encryption)
        {
            bool isProductFileCreated = false;

            if (!string.IsNullOrWhiteSpace(batchInfo.Path))
            {
                DateTime createProductFileTaskStartedAt = DateTime.UtcNow;
                isProductFileCreated = await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateProductFileRequestStart,
                        EventIds.CreateProductFileRequestCompleted,
                        "Create product file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                        async () =>
                        {
                            return await fulfilmentAncillaryFiles.CreateProductFile(batchInfo, salesCatalogueDataResponse, scsRequestDateTime, encryption);
                        },
                        batchInfo.BatchId, batchInfo.CorrelationId);

                DateTime createProductFileTaskCompletedAt = DateTime.UtcNow;
                monitorHelper.MonitorRequest("Create Product File Task", createProductFileTaskStartedAt, createProductFileTaskCompletedAt, batchInfo.CorrelationId, null, null, null, batchInfo.BatchId);
            }

            return isProductFileCreated;
        }

        public async Task CreateSerialEncFile(BatchInfo batchInfo)
        {
            await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateSerialFileRequestStart,
                      EventIds.CreateSerialFileRequestCompleted,
                      "Create serial enc file request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                      async () =>
                      {
                          return await fulfilmentAncillaryFiles.CreateSerialEncFile(batchInfo);
                      },
                  batchInfo.BatchId, batchInfo.CorrelationId);
        }
    }
}
