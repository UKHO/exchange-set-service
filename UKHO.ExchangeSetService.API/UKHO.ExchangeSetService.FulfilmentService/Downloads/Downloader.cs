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
        public async Task<bool> DownloadReadMeFileAsync(BatchInfo batchInfo)
        {
            bool isDownloadReadMeFileSuccess = false;
            DateTime createReadMeFileTaskStartedAt = DateTime.UtcNow;

            logger.LogInformation(EventIds.SearchDownloadReadmeCacheEventStart.ToEventId(), "Cache Search and Download readme.txt file started for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchInfo.BatchId, batchInfo.CorrelationId);

            try
            {
                isDownloadReadMeFileSuccess = await fulfilmentFileShareService.DownloadReadMeFileFromCacheAsync(batchInfo.BatchId, batchInfo.Path, batchInfo.CorrelationId);
                if (isDownloadReadMeFileSuccess)
                {
                    logger.LogInformation(EventIds.SearchDownloadReadmeCacheEventCompleted.ToEventId(), "Cache Search and Download readme.txt file completed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchInfo.BatchId, batchInfo.CorrelationId);
                    DateTime createReadMeFileTaskCompletedAt = DateTime.UtcNow;
                    monitorHelper.MonitorRequest("Download ReadMe File Task", createReadMeFileTaskStartedAt, createReadMeFileTaskCompletedAt, batchInfo.CorrelationId, null, null, null, batchInfo.BatchId);
                }
                else
                {
                    logger.LogInformation(EventIds.ReadMeTextFileNotFound.ToEventId(), "Cache Search and Download readme.txt file not found in blob cache for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchInfo.BatchId, batchInfo.CorrelationId);
                    isDownloadReadMeFileSuccess = await DownloadReadMeFileFromFssAsync(batchInfo);
                }

                if (!isDownloadReadMeFileSuccess)
                {
                    logger.LogError(EventIds.ErrorInDownloadReadMeFile.ToEventId(), "Error while downloading readme.txt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batchInfo.BatchId, batchInfo.CorrelationId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.ErrorInDownloadReadMeFile.ToEventId(), ex, "Error while downloading readme.txt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} and Exception:{Message}", batchInfo.BatchId, batchInfo.CorrelationId, ex.Message);
            }
            return isDownloadReadMeFileSuccess;
        }

        public async Task<bool> DownloadReadMeFileFromFssAsync(BatchInfo batchInfo)
        {
            bool isDownloadReadMeFileSuccess = false;
            string readMeFilePath = await logger.LogStartEndAndElapsedTimeAsync(EventIds.QueryFileShareServiceReadMeFileRequestStart,
                EventIds.QueryFileShareServiceReadMeFileRequestCompleted,
                "File share service search query request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    return await fulfilmentFileShareService.SearchReadMeFilePath(batchInfo.BatchId, batchInfo.CorrelationId);
                },
                batchInfo.BatchId, batchInfo.CorrelationId);

            if (!string.IsNullOrWhiteSpace(readMeFilePath))
            {
                DateTime createReadMeFileTaskStartedAt = DateTime.UtcNow;
                isDownloadReadMeFileSuccess = await logger.LogStartEndAndElapsedTimeAsync(EventIds.DownloadReadMeFileRequestStart,
                   EventIds.DownloadReadMeFileRequestCompleted,
                   "File share service download request for readme file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                   async () =>
                   {
                       return await fulfilmentFileShareService.DownloadReadMeFileFromFssAsync(readMeFilePath, batchInfo.BatchId, batchInfo.Path, batchInfo.CorrelationId);
                   },
                batchInfo.BatchId, batchInfo.CorrelationId);

                DateTime createReadMeFileTaskCompletedAt = DateTime.UtcNow;
                monitorHelper.MonitorRequest("Download ReadMe File Task", createReadMeFileTaskStartedAt, createReadMeFileTaskCompletedAt, batchInfo.CorrelationId, null, null, null, batchInfo.BatchId);
            }

            return isDownloadReadMeFileSuccess;
        }

        public async Task<bool> DownloadIhoCrtFile(BatchInfo batchInfo)
        {
            bool isDownloadIhoCrtFileSuccess = false;
            string ihoCrtFilePath = await logger.LogStartEndAndElapsedTimeAsync(EventIds.QueryFileShareServiceIhoCrtFileRequestStart,
                EventIds.QueryFileShareServiceIhoCrtFileRequestCompleted,
                "File share service search query request for IHO.crt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    return await fulfilmentFileShareService.SearchIhoCrtFilePath(batchInfo.BatchId, batchInfo.CorrelationId);
                },
                batchInfo.BatchId, batchInfo.CorrelationId);

            if (!string.IsNullOrWhiteSpace(ihoCrtFilePath))
            {
                DateTime createIhoCrtFileTaskStartedAt = DateTime.UtcNow;
                isDownloadIhoCrtFileSuccess = await logger.LogStartEndAndElapsedTimeAsync(EventIds.DownloadIhoCrtFileRequestStart,
                    EventIds.DownloadIhoCrtFileRequestCompleted,
                    "File share service download request for IHO.crt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                    async () =>
                    {
                        return await fulfilmentFileShareService.DownloadIhoCrtFile(ihoCrtFilePath, batchInfo.BatchId, batchInfo.Path, batchInfo.CorrelationId);
                    },
                    batchInfo.BatchId, batchInfo.CorrelationId);

                DateTime createIhoCrtFileTaskCompletedAt = DateTime.UtcNow;
                monitorHelper.MonitorRequest("Download IHO.crt File Task", createIhoCrtFileTaskStartedAt, createIhoCrtFileTaskCompletedAt, batchInfo.CorrelationId, null, null, null, batchInfo.BatchId);
            }

            return isDownloadIhoCrtFileSuccess;
        }

        public async Task<bool> DownloadIhoPubFile(BatchInfo batchInfo)
        {
            bool isDownloadIhoPubFileSuccess = false;
            string ihoPubFilePath = await logger.LogStartEndAndElapsedTimeAsync(EventIds.QueryFileShareServiceIhoPubFileRequestStart,
                EventIds.QueryFileShareServiceIhoPubFileRequestCompleted,
                "File share service search query request for IHO.pub file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    return await fulfilmentFileShareService.SearchIhoPubFilePath(batchInfo.BatchId, batchInfo.CorrelationId);
                },
                batchInfo.BatchId, batchInfo.CorrelationId);

            if (!string.IsNullOrWhiteSpace(ihoPubFilePath))
            {
                DateTime createIhoPubFileTaskStartedAt = DateTime.UtcNow;
                isDownloadIhoPubFileSuccess = await logger.LogStartEndAndElapsedTimeAsync(EventIds.DownloadIhoPubFileRequestStart,
                    EventIds.DownloadIhoPubFileRequestCompleted,
                    "File share service download request for IHO.pub file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                    async () =>
                    {
                        return await fulfilmentFileShareService.DownloadIhoPubFile(ihoPubFilePath, batchInfo.BatchId, batchInfo.BatchId, batchInfo.CorrelationId);
                    },
                    batchInfo.BatchId, batchInfo.CorrelationId);

                DateTime createIhoPubFileTaskCompletedAt = DateTime.UtcNow;
                monitorHelper.MonitorRequest("Download Iho Pub File Task", createIhoPubFileTaskStartedAt, createIhoPubFileTaskCompletedAt, batchInfo.CorrelationId, null, null, null, batchInfo.BatchId);
            }

            return isDownloadIhoPubFileSuccess;
        }

        public async Task DownloadInfoFolderFiles(BatchInfo batchInfo)
        {
            IEnumerable<BatchFile> fileDetails = await logger.LogStartEndAndElapsedTimeAsync(EventIds.DownloadInfoFolderRequestStart,
                  EventIds.DownloadInfoFolderRequestCompleted,
                  "File share service search query request for Info folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                  async () => await fulfilmentFileShareService.SearchFolderDetails(batchInfo.BatchId, batchInfo.CorrelationId, fileShareServiceConfig.Value.Info),
                  batchInfo.BatchId, batchInfo.CorrelationId);

            if (fileDetails != null && fileDetails.Any())
            {
                DateTime createInfoFolderFileTaskStartedAt = DateTime.UtcNow;
                await logger.LogStartEndAndElapsedTimeAsync(EventIds.DownloadInfoFolderRequestStart,
                   EventIds.DownloadInfoFolderRequestCompleted,
                   "File share service download request for Info folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                   async () => await fulfilmentFileShareService.DownloadFolderDetails(batchInfo.BatchId, batchInfo.CorrelationId, fileDetails, batchInfo.Path),
                   batchInfo.BatchId, batchInfo.CorrelationId);

                DateTime createInfoFolderFileTaskCompletedAt = DateTime.UtcNow;
                monitorHelper.MonitorRequest("Download Info Folder File Task", createInfoFolderFileTaskStartedAt, createInfoFolderFileTaskCompletedAt, batchInfo.CorrelationId, null, null, null, batchInfo.BatchId);
            }
        }

        public async Task DownloadAdcFolderFiles(BatchInfo batchInfo)
        {
            IEnumerable<BatchFile> fileDetails = await logger.LogStartEndAndElapsedTimeAsync(EventIds.QueryFileShareServiceAdcFolderFilesRequestStart,
                  EventIds.QueryFileShareServiceAdcFolderFilesRequestCompleted,
                  "File share service search query request for Adc folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                  async () => await fulfilmentFileShareService.SearchFolderDetails(batchInfo.BatchId, batchInfo.CorrelationId, fileShareServiceConfig.Value.Adc),
                  batchInfo.BatchId, batchInfo.CorrelationId);

            if (fileDetails != null && fileDetails.Any())
            {
                DateTime createAdcFolderFilesTaskStartedAt = DateTime.UtcNow;
                await logger.LogStartEndAndElapsedTimeAsync(EventIds.DownloadAdcFolderFilesStart,
                   EventIds.DownloadAdcFolderFilesCompleted,
                   "File share service download request for Adc folder files for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                   async () => await fulfilmentFileShareService.DownloadFolderDetails(batchInfo.BatchId, batchInfo.CorrelationId, fileDetails, batchInfo.Path),
                   batchInfo.BatchId, batchInfo.CorrelationId);

                DateTime createAdcFolderFilesTaskCompletedAt = DateTime.UtcNow;
                monitorHelper.MonitorRequest("Download Adc Folder File Task", createAdcFolderFilesTaskStartedAt, createAdcFolderFilesTaskCompletedAt, batchInfo.CorrelationId, null, null, null, batchInfo.BatchId);
            }
        }
        public async Task DownloadLargeMediaReadMeFile(BatchInfo batchInfo)
        {
            var baseDirectory = fileSystemHelper.GetDirectoryInfo(batchInfo.Path)
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
                ParallelCreateFolderTasks.Add(DownloadReadMeFileAsync(batchInfo));
            });
            await Task.WhenAll(ParallelCreateFolderTasks);
            ParallelCreateFolderTasks.Clear();
        }
    }
}
