using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.FulfilmentService.Services;

namespace UKHO.ExchangeSetService.FulfilmentService.Downloads
{
    public class Downloader(
        ILogger<FulfilmentDataService> logger,
        IMonitorHelper monitorHelper,
        IFileSystemHelper fileSystemHelper,
        IFulfilmentFileShareService fulfilmentFileShareService,
        IOptions<FileShareServiceConfiguration> fileShareServiceConfig) : IDownloader
    {
        public async Task<bool> DownloadReadMeFileAsync(string batchId, string exchangeSetRootPath, string correlationId)
        {
            bool isDownloadReadMeFileSuccess = false;
            DateTime createReadMeFileTaskStartedAt = DateTime.UtcNow;

            logger.LogInformation(EventIds.SearchDownloadReadmeCacheEventStart.ToEventId(), "Cache Search and Download readme.txt file started for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);

            try
            {
                isDownloadReadMeFileSuccess = await fulfilmentFileShareService.DownloadReadMeFileFromCacheAsync(batchId, exchangeSetRootPath, correlationId);
                if (isDownloadReadMeFileSuccess)
                {
                    logger.LogInformation(EventIds.SearchDownloadReadmeCacheEventCompleted.ToEventId(), "Cache Search and Download readme.txt file completed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                    DateTime createReadMeFileTaskCompletedAt = DateTime.UtcNow;
                    monitorHelper.MonitorRequest("Download ReadMe File Task", createReadMeFileTaskStartedAt, createReadMeFileTaskCompletedAt, correlationId, null, null, null, batchId);
                }
                else
                {
                    logger.LogInformation(EventIds.ReadMeTextFileNotFound.ToEventId(), "Cache Search and Download readme.txt file not found in blob cache for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                    isDownloadReadMeFileSuccess = await DownloadReadMeFileFromFssAsync(batchId, exchangeSetRootPath, correlationId);
                }

                if (!isDownloadReadMeFileSuccess)
                {
                    logger.LogError(EventIds.ErrorInDownloadReadMeFile.ToEventId(), "Error while downloading readme.txt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.ErrorInDownloadReadMeFile.ToEventId(), ex, "Error while downloading readme.txt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} and Exception:{Message}", batchId, correlationId, ex.Message);
            }
            return isDownloadReadMeFileSuccess;
        }

        public async Task<bool> DownloadReadMeFileFromFssAsync(string batchId, string exchangeSetRootPath, string correlationId)
        {
            bool isDownloadReadMeFileSuccess = false;
            string readMeFilePath = await logger.LogStartEndAndElapsedTimeAsync(EventIds.QueryFileShareServiceReadMeFileRequestStart,
                EventIds.QueryFileShareServiceReadMeFileRequestCompleted,
                "File share service search query request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    return await fulfilmentFileShareService.SearchReadMeFilePath(batchId, correlationId);
                },
                batchId, correlationId);

            if (!string.IsNullOrWhiteSpace(readMeFilePath))
            {
                DateTime createReadMeFileTaskStartedAt = DateTime.UtcNow;
                isDownloadReadMeFileSuccess = await logger.LogStartEndAndElapsedTimeAsync(EventIds.DownloadReadMeFileRequestStart,
                   EventIds.DownloadReadMeFileRequestCompleted,
                   "File share service download request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                   async () =>
                   {
                       return await fulfilmentFileShareService.DownloadReadMeFileFromFssAsync(readMeFilePath, batchId, exchangeSetRootPath, correlationId);
                   },
                batchId, correlationId);

                DateTime createReadMeFileTaskCompletedAt = DateTime.UtcNow;
                monitorHelper.MonitorRequest("Download ReadMe File Task", createReadMeFileTaskStartedAt, createReadMeFileTaskCompletedAt, correlationId, null, null, null, batchId);
            }

            return isDownloadReadMeFileSuccess;
        }

        public async Task<bool> DownloadIhoCrtFile(string batchId, string aioExchangeSetPath, string correlationId)
        {
            bool isDownloadIhoCrtFileSuccess = false;
            string ihoCrtFilePath = await logger.LogStartEndAndElapsedTimeAsync(EventIds.QueryFileShareServiceIhoCrtFileRequestStart,
                EventIds.QueryFileShareServiceIhoCrtFileRequestCompleted,
                "File share service search query request for IHO.crt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    return await fulfilmentFileShareService.SearchIhoCrtFilePath(batchId, correlationId);
                },
                batchId, correlationId);

            if (!string.IsNullOrWhiteSpace(ihoCrtFilePath))
            {
                DateTime createIhoCrtFileTaskStartedAt = DateTime.UtcNow;
                isDownloadIhoCrtFileSuccess = await logger.LogStartEndAndElapsedTimeAsync(EventIds.DownloadIhoCrtFileRequestStart,
                    EventIds.DownloadIhoCrtFileRequestCompleted,
                    "File share service download request for IHO.crt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                    async () =>
                    {
                        return await fulfilmentFileShareService.DownloadIhoCrtFile(ihoCrtFilePath, batchId, aioExchangeSetPath, correlationId);
                    },
                    batchId, correlationId);

                DateTime createIhoCrtFileTaskCompletedAt = DateTime.UtcNow;
                monitorHelper.MonitorRequest("Download IHO.crt File Task", createIhoCrtFileTaskStartedAt, createIhoCrtFileTaskCompletedAt, correlationId, null, null, null, batchId);
            }

            return isDownloadIhoCrtFileSuccess;
        }

        public async Task<bool> DownloadIhoPubFile(string batchId, string exchangeSetRootPath, string correlationId)
        {
            bool isDownloadIhoPubFileSuccess = false;
            string ihoPubFilePath = await logger.LogStartEndAndElapsedTimeAsync(EventIds.QueryFileShareServiceIhoPubFileRequestStart,
                EventIds.QueryFileShareServiceIhoPubFileRequestCompleted,
                "File share service search query request for IHO.pub file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    return await fulfilmentFileShareService.SearchIhoPubFilePath(batchId, correlationId);
                },
                batchId, correlationId);

            if (!string.IsNullOrWhiteSpace(ihoPubFilePath))
            {
                DateTime createIhoPubFileTaskStartedAt = DateTime.UtcNow;
                isDownloadIhoPubFileSuccess = await logger.LogStartEndAndElapsedTimeAsync(EventIds.DownloadIhoPubFileRequestStart,
                    EventIds.DownloadIhoPubFileRequestCompleted,
                    "File share service download request for IHO.pub file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                    async () =>
                    {
                        return await fulfilmentFileShareService.DownloadIhoPubFile(ihoPubFilePath, batchId, exchangeSetRootPath, correlationId);
                    },
                    batchId, correlationId);

                DateTime createIhoPubFileTaskCompletedAt = DateTime.UtcNow;
                monitorHelper.MonitorRequest("Download Iho Pub File Task", createIhoPubFileTaskStartedAt, createIhoPubFileTaskCompletedAt, correlationId, null, null, null, batchId);
            }

            return isDownloadIhoPubFileSuccess;
        }

        public async Task DownloadInfoFolderFiles(string batchId, string exchangeSetInfoPath, string correlationId)
        {
            IEnumerable<BatchFile> fileDetails = await logger.LogStartEndAndElapsedTimeAsync(EventIds.DownloadInfoFolderRequestStart,
                  EventIds.DownloadInfoFolderRequestCompleted,
                  "File share service search query request for Info folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                  async () => await fulfilmentFileShareService.SearchFolderDetails(batchId, correlationId, fileShareServiceConfig.Value.Info),
                  batchId, correlationId);

            if (fileDetails != null && fileDetails.Any())
            {
                DateTime createInfoFolderFileTaskStartedAt = DateTime.UtcNow;
                await logger.LogStartEndAndElapsedTimeAsync(EventIds.DownloadInfoFolderRequestStart,
                   EventIds.DownloadInfoFolderRequestCompleted,
                   "File share service download request for Info folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                   async () => await fulfilmentFileShareService.DownloadFolderDetails(batchId, correlationId, fileDetails, exchangeSetInfoPath),
                   batchId, correlationId);

                DateTime createInfoFolderFileTaskCompletedAt = DateTime.UtcNow;
                monitorHelper.MonitorRequest("Download Info Folder File Task", createInfoFolderFileTaskStartedAt, createInfoFolderFileTaskCompletedAt, correlationId, null, null, null, batchId);
            }
        }

        public async Task DownloadAdcFolderFiles(string batchId, string exchangeSetAdcPath, string correlationId)
        {
            IEnumerable<BatchFile> fileDetails = await logger.LogStartEndAndElapsedTimeAsync(EventIds.QueryFileShareServiceAdcFolderFilesRequestStart,
                  EventIds.QueryFileShareServiceAdcFolderFilesRequestCompleted,
                  "File share service search query request for Adc folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                  async () => await fulfilmentFileShareService.SearchFolderDetails(batchId, correlationId, fileShareServiceConfig.Value.Adc),
                  batchId, correlationId);

            if (fileDetails != null && fileDetails.Any())
            {
                DateTime createAdcFolderFilesTaskStartedAt = DateTime.UtcNow;
                await logger.LogStartEndAndElapsedTimeAsync(EventIds.DownloadAdcFolderFilesStart,
                   EventIds.DownloadAdcFolderFilesCompleted,
                   "File share service download request for Adc folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                   async () => await fulfilmentFileShareService.DownloadFolderDetails(batchId, correlationId, fileDetails, exchangeSetAdcPath),
                   batchId, correlationId);

                DateTime createAdcFolderFilesTaskCompletedAt = DateTime.UtcNow;
                monitorHelper.MonitorRequest("Download Adc Folder File Task", createAdcFolderFilesTaskStartedAt, createAdcFolderFilesTaskCompletedAt, correlationId, null, null, null, batchId);
            }
        }
        public async Task DownloadLargeMediaReadMeFile(string batchId, string exchangeSetPath, string correlationId)
        {
            var baseDirectory = fileSystemHelper.GetDirectoryInfo(exchangeSetPath)
                       .Where(di => di.Name.StartsWith("B") && di.Name.Length <= 3 && CommonHelper.IsNumeric(di.Name[^(di.Name.Length - 1)..]));

            var encFolderList = new List<string>();
            foreach (var directory in baseDirectory)
            {
                var encFolder = Path.Combine(directory.ToString(), fileShareServiceConfig.Value.EncRoot);
                encFolderList.Add(encFolder);
            }
            var ParallelCreateFolderTasks = new List<Task> { };

            Parallel.ForEach(encFolderList, encFolder =>
            {
                ParallelCreateFolderTasks.Add(DownloadReadMeFileAsync(batchId, encFolder, correlationId));
            });
            await Task.WhenAll(ParallelCreateFolderTasks);
            ParallelCreateFolderTasks.Clear();
        }
    }
}
