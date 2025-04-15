using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace UKHO.ExchangeSetService.FulfilmentService.FileCreation
{
    public class FileBuilder(
        ILogger<FulfilmentDataService> logger,
        IMonitorHelper monitorHelper,
        IFileSystemHelper fileSystemHelper,
        IDownloader download,
        IFulfilmentAncillaryFiles fulfilmentAncillaryFiles,
        IOptions<FileShareServiceConfiguration> fileShareServiceConfig) : IFileBuilder
    {

        public async Task<bool> CreateAncillaryFilesForAio(string batchId, string aioExchangeSetPath, string correlationId, SalesCatalogueDataResponse salesCatalogueDataResponse, DateTime scsRequestDateTime, SalesCatalogueProductResponse salesCatalogueProductResponse, List<FulfilmentDataResponse> listFulfilmentAioData)
        {
            var exchangeSetRootPath = Path.Combine(aioExchangeSetPath, fileShareServiceConfig.Value.EncRoot);
            var exchangeSetInfoPath = Path.Combine(aioExchangeSetPath, fileShareServiceConfig.Value.Info);

            return
            await download.DownloadReadMeFileAsync(batchId, exchangeSetRootPath, correlationId) &&
            await download.DownloadIhoCrtFile(batchId, aioExchangeSetPath, correlationId) &&
            await download.DownloadIhoPubFile(batchId, aioExchangeSetPath, correlationId) &&
            await CreateSerialAioFile(batchId, aioExchangeSetPath, correlationId, salesCatalogueDataResponse) &&
            await CreateProductFileForAio(batchId, exchangeSetInfoPath, correlationId, salesCatalogueDataResponse, scsRequestDateTime) &&
            await CreateCatalogFileForAio(batchId, exchangeSetRootPath, correlationId, listFulfilmentAioData, salesCatalogueDataResponse, salesCatalogueProductResponse);
        }

        public async Task<bool> CreateSerialAioFile(string batchId, string aioExchangeSetPath, string correlationId, SalesCatalogueDataResponse salesCatalogueDataResponse)
        {
            bool isSerialAioCreated = await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateSerialAioFileRequestStart,
                      EventIds.CreateSerialAioFileRequestCompleted,
                      "Create serial aio file request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                      async () =>
                      {
                          return await fulfilmentAncillaryFiles.CreateSerialAioFile(batchId, aioExchangeSetPath, correlationId, salesCatalogueDataResponse);
                      },
                  batchId, correlationId);

            return isSerialAioCreated;
        }

        public async Task<bool> CreateProductFileForAio(string batchId, string exchangeSetInfoPath, string correlationId, SalesCatalogueDataResponse salesCatalogueDataResponse, DateTime scsRequestDateTime)
        {
            bool isProductFileCreated = false;

            if (!string.IsNullOrWhiteSpace(exchangeSetInfoPath))
            {
                DateTime createProductFileTaskStartedAt = DateTime.UtcNow;
                isProductFileCreated = await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateProductFileRequestForAioStart,
                        EventIds.CreateProductFileRequestForAioCompleted,
                        "Create aio exchange set product file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                        async () =>
                        {
                            return await fulfilmentAncillaryFiles.CreateProductFile(batchId, exchangeSetInfoPath, correlationId, salesCatalogueDataResponse, scsRequestDateTime);
                        },
                        batchId, correlationId);

                DateTime createProductFileTaskCompletedAt = DateTime.UtcNow;
                monitorHelper.MonitorRequest("Create Product File Task", createProductFileTaskStartedAt, createProductFileTaskCompletedAt, correlationId, null, null, null, batchId);
            }

            return isProductFileCreated;
        }

        public async Task<bool> CreateCatalogFileForAio(string batchId, string exchangeSetRootPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse)
        {
            bool isFileCreated = false;

            if (!string.IsNullOrWhiteSpace(exchangeSetRootPath))
            {
                DateTime createCatalogFileForAioTaskStartedAt = DateTime.UtcNow;
                isFileCreated = await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateCatalogFileForAioRequestStart,
                        EventIds.CreateCatalogFileForAioRequestCompleted,
                        "Create AIO exchange set catalog file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                        async () =>
                        {
                            return await fulfilmentAncillaryFiles.CreateCatalogFile(batchId, exchangeSetRootPath, correlationId, listFulfilmentData, salesCatalogueDataResponse, salesCatalogueProductResponse);
                        },
                        batchId, correlationId);

                DateTime createCatalogFileForAioTaskCompletedAt = DateTime.UtcNow;
                monitorHelper.MonitorRequest("Create Catalog File Task", createCatalogFileForAioTaskStartedAt, createCatalogFileForAioTaskCompletedAt, correlationId, null, null, null, batchId);
            }

            return isFileCreated;
        }

        public async Task<bool> CreateLargeMediaSerialEncFile(string batchId, string exchangeSetPath, string rootfolder, string correlationId)
        {
            DateTime createLargeMediaSerialEncFileTaskStartedAt = DateTime.UtcNow;

            return await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateSerialFileRequestStart,
                      EventIds.CreateSerialFileRequestCompleted,
                      "Create large media serial enc file request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                      async () =>
                      {
                          var rootLastDirectoryPath = fileSystemHelper.GetDirectoryInfo(exchangeSetPath)
                                                  .LastOrDefault(di => di.Name.StartsWith("M0"));

                          var baseDirectoryies = fileSystemHelper.GetDirectoryInfo(Path.Combine(exchangeSetPath, rootfolder))
                                                  .Where(di => di.Name.StartsWith("B") && di.Name.Length <= 3 && CommonHelper.IsNumeric(di.Name[^(di.Name.Length - 1)..]));

                          var baseLastDirectory = fileSystemHelper.GetDirectoryInfo(rootLastDirectoryPath?.ToString())
                                                  .LastOrDefault(di => di.Name.StartsWith("B") && di.Name.Length <= 3 && CommonHelper.IsNumeric(di.Name[^(di.Name.Length - 1)..]));

                          string lastBaseDirectoryNumber = baseLastDirectory.ToString().Replace(Path.Combine(rootLastDirectoryPath.ToString(), "B"), "");

                          var ParallelBaseFolderTasks = new List<Task<bool>> { };
                          Parallel.ForEach(baseDirectoryies, baseDirectoryFolder =>
                          {
                              string baseDirectoryNumber = baseDirectoryFolder.ToString().Replace(Path.Combine(exchangeSetPath, rootfolder, "B"), "");
                              ParallelBaseFolderTasks.Add(fulfilmentAncillaryFiles.CreateLargeMediaSerialEncFile(batchId, baseDirectoryFolder.ToString(), correlationId, baseDirectoryNumber.ToString(), lastBaseDirectoryNumber));
                          });
                          await Task.WhenAll(ParallelBaseFolderTasks);

                          DateTime createLargeMediaSerialEncFileTaskCompletedAt = DateTime.UtcNow;
                          monitorHelper.MonitorRequest("Create Large Media Serial Enc File Task", createLargeMediaSerialEncFileTaskStartedAt, createLargeMediaSerialEncFileTaskCompletedAt, correlationId, null, null, null, batchId);

                          return await Task.FromResult(ParallelBaseFolderTasks.All(x => x.Result.Equals(true)));
                      },
                  batchId, correlationId);
        }

        public async Task<bool> CreateLargeMediaExchangesetCatalogFile(string batchId, string exchangeSetPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse)
        {
            var baseDirectory = fileSystemHelper.GetDirectoryInfo(exchangeSetPath)
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
                ParallelCreateFolderTasks.Add(fulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(batchId, encFolder, correlationId, fulfilmentDataBasedonCountryCode, salesCatalogueDataResponse, salesCatalogueProductResponse));
            });
            await Task.WhenAll(ParallelCreateFolderTasks);
            var isCreateFolderTasksSuccessful = await Task.FromResult(ParallelCreateFolderTasks.All(x => x.Result.Equals(true)));
            ParallelCreateFolderTasks.Clear();

            return isCreateFolderTasksSuccessful;
        }

        public async Task<bool> CreateCatalogFile(string batchId, string exchangeSetRootPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueDataResponse salesCatalogueDataResponse, SalesCatalogueProductResponse salesCatalogueProductResponse)
        {
            bool isFileCreated = false;

            if (!string.IsNullOrWhiteSpace(exchangeSetRootPath))
            {
                DateTime createCatalogFileTaskStartedAt = DateTime.UtcNow;
                isFileCreated = await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateCatalogFileRequestStart,
                        EventIds.CreateCatalogFileRequestCompleted,
                        "Create catalog file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                        async () =>
                        {
                            return await fulfilmentAncillaryFiles.CreateCatalogFile(batchId, exchangeSetRootPath, correlationId, listFulfilmentData, salesCatalogueDataResponse, salesCatalogueProductResponse);
                        },
                        batchId, correlationId);

                DateTime createCatalogFileTaskCompletedAt = DateTime.UtcNow;
                monitorHelper.MonitorRequest("Create Catalog File Task", createCatalogFileTaskStartedAt, createCatalogFileTaskCompletedAt, correlationId, null, null, null, batchId);
            }

            return isFileCreated;
        }

        public async Task<bool> CreateProductFile(string batchId, string exchangeSetInfoPath, string correlationId, SalesCatalogueDataResponse salesCatalogueDataResponse, DateTime scsRequestDateTime, bool encryption)
        {
            bool isProductFileCreated = false;

            if (!string.IsNullOrWhiteSpace(exchangeSetInfoPath))
            {
                DateTime createProductFileTaskStartedAt = DateTime.UtcNow;
                isProductFileCreated = await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateProductFileRequestStart,
                        EventIds.CreateProductFileRequestCompleted,
                        "Create product file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                        async () =>
                        {
                            return await fulfilmentAncillaryFiles.CreateProductFile(batchId, exchangeSetInfoPath, correlationId, salesCatalogueDataResponse, scsRequestDateTime, encryption);
                        },
                        batchId, correlationId);

                DateTime createProductFileTaskCompletedAt = DateTime.UtcNow;
                monitorHelper.MonitorRequest("Create Product File Task", createProductFileTaskStartedAt, createProductFileTaskCompletedAt, correlationId, null, null, null, batchId);
            }

            return isProductFileCreated;
        }

        public async Task CreateSerialEncFile(string batchId, string exchangeSetPath, string correlationId)
        {
            await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateSerialFileRequestStart,
                      EventIds.CreateSerialFileRequestCompleted,
                      "Create serial enc file request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                      async () =>
                      {
                          return await fulfilmentAncillaryFiles.CreateSerialEncFile(batchId, exchangeSetPath, correlationId);
                      },
                  batchId, correlationId);
        }


        public async Task CreateAncillaryFiles(string batchId, string exchangeSetPath, string correlationId, List<FulfilmentDataResponse> listFulfilmentData, SalesCatalogueProductResponse salecatalogueProductResponse, DateTime scsRequestDateTime, SalesCatalogueDataResponse salesCatalogueEssDataResponse, bool encryption)
        {
            var exchangeSetRootPath = Path.Combine(exchangeSetPath, fileShareServiceConfig.Value.EncRoot);
            var exchangeSetInfoPath = Path.Combine(exchangeSetPath, fileShareServiceConfig.Value.Info);

            await CreateProductFile(batchId, exchangeSetInfoPath, correlationId, salesCatalogueEssDataResponse, scsRequestDateTime, encryption);
            await CreateSerialEncFile(batchId, exchangeSetPath, correlationId);
            await download.DownloadReadMeFileAsync(batchId, exchangeSetRootPath, correlationId);
            await CreateCatalogFile(batchId, exchangeSetRootPath, correlationId, listFulfilmentData, salesCatalogueEssDataResponse, salecatalogueProductResponse);
        }
    }
}
