// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Helpers.Auth;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public class FileShareUploadService(
        ILogger<FileShareService> logger,
        IMonitorHelper monitorHelper,
        IAuthFssTokenProvider authFssTokenProvider,
        IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
        IFileShareServiceClient fileShareServiceClient,
        IFileSystemHelper fileSystemHelper)
         : IFileShareUploadService
    {
        public async Task<bool> UploadFileToFileShareService(string batchId, string exchangeSetZipRootPath, string correlationId, string fileName)
        {
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            var isUploadZipFile = false;
            var uploadZipFileTaskStartedAt = DateTime.UtcNow;
            var customFileInfo = fileSystemHelper.GetFileInfo(Path.Combine(exchangeSetZipRootPath, fileName));

            var fileCreateMetaData = new FileCreateMetaData()
            {
                AccessToken = accessToken,
                BatchId = batchId,
                FileName = customFileInfo.Name,
                Length = customFileInfo.Length
            };
            var isZipFileCreated = await CreateFile(fileCreateMetaData, accessToken, correlationId);
            if (isZipFileCreated)
            {
                var isWriteBlock = await UploadAndWriteBlock(batchId, correlationId, accessToken, customFileInfo);
                if (isWriteBlock)
                {
                    isUploadZipFile = true;
                    var uploadZipFileTaskCompletedAt = DateTime.UtcNow;
                    monitorHelper.MonitorRequest("Upload Zip File Task", uploadZipFileTaskStartedAt, uploadZipFileTaskCompletedAt, correlationId, null, null, null, batchId);
                }
            }
            return isUploadZipFile;
        }

        public async Task<bool> UploadLargeMediaFileToFileShareService(string batchId, string exchangeSetZipPath, string correlationId, string fileName)
        {
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            var isWriteBlock = false;
            var uploadZipFileTaskStartedAt = DateTime.UtcNow;
            var customFileInfo = fileSystemHelper.GetFileInfo(Path.Combine(exchangeSetZipPath, fileName));

            var fileCreateMetaData = new FileCreateMetaData()
            {
                AccessToken = accessToken,
                BatchId = batchId,
                FileName = customFileInfo.Name,
                Length = customFileInfo.Length
            };
            var isZipFileCreated = await CreateFile(fileCreateMetaData, accessToken, correlationId);
            if (isZipFileCreated)
            {
                isWriteBlock = await UploadAndWriteBlock(batchId, correlationId, accessToken, customFileInfo);
                if (isWriteBlock)
                {
                    var uploadZipFileTaskCompletedAt = DateTime.UtcNow;
                    monitorHelper.MonitorRequest("Upload Zip File Task", uploadZipFileTaskStartedAt, uploadZipFileTaskCompletedAt, correlationId, null, null, null, batchId);
                }
            }
            return isWriteBlock;
        }

        private Task<bool> CreateFile(FileCreateMetaData fileCreateMetaData, string accessToken, string correlationId)
        {
            return logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateFileInBatchStart, EventIds.CreateFileInBatchCompleted,
                    "File:{FileName} creation in batch for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                    async () =>
                    {
                        HttpResponseMessage httpResponse;

                        var mimetype = fileCreateMetaData.FileName.Contains("zip") ? "application/zip" : "text/plain";

                        httpResponse = await fileShareServiceClient.AddFileInBatchAsync(HttpMethod.Post, new FileCreateModel(), accessToken, fileShareServiceConfig.Value.BaseUrl, fileCreateMetaData.BatchId, fileCreateMetaData.FileName, fileCreateMetaData.Length, mimetype, correlationId);
                        if (!httpResponse.IsSuccessStatusCode)
                        {
                            logger.LogError(EventIds.CreateFileInBatchNonOkResponse.ToEventId(), "Error while creating/adding file:{FileName} in batch with uri {RequestUri} responded with {StatusCode} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", fileCreateMetaData.FileName, httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, fileCreateMetaData.BatchId, correlationId);
                            throw new FulfilmentException(EventIds.CreateFileInBatchNonOkResponse.ToEventId());
                        }
                        return true;
                    }, fileCreateMetaData.FileName, fileCreateMetaData.BatchId, correlationId);
        }

        private async Task<bool> UploadAndWriteBlock(string batchId, string correlationId, string accessToken, CustomFileInfo customFileInfo)
        {
            var blockIdList = await UploadBlockFile(batchId, customFileInfo, accessToken, correlationId);
            var writeBlocksToFileMetaData = new WriteBlocksToFileMetaData()
            {
                BatchId = batchId,
                FileName = customFileInfo.Name,
                AccessToken = accessToken,
                BlockIds = blockIdList
            };
            return await WriteBlockFile(writeBlocksToFileMetaData, correlationId);
        }

        private async Task<List<string>> UploadBlockFile(string batchId, CustomFileInfo customFileInfo, string accessToken, string correlationId)
        {
            var uploadMessage = new UploadMessage()
            {
                UploadSize = customFileInfo.Length,
                BlockSizeInMultipleOfKBs = fileShareServiceConfig.Value.BlockSizeInMultipleOfKBs
            };
            long blockSizeInMultipleOfKBs = uploadMessage.BlockSizeInMultipleOfKBs <= 0 || uploadMessage.BlockSizeInMultipleOfKBs > 4096
                ? 1024
                : uploadMessage.BlockSizeInMultipleOfKBs;
            var blockSize = blockSizeInMultipleOfKBs * 1024;
            var blockIdList = new List<string>();
            var ParallelBlockUploadTasks = new List<Task>();
            long uploadedBytes = 0;
            var blockNum = 0;
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            while (uploadedBytes < customFileInfo.Length)
            {
                blockNum++;
                var readBlockSize = (int)(customFileInfo.Length - uploadedBytes <= blockSize ? customFileInfo.Length - uploadedBytes : blockSize);
                var blockId = CommonHelper.GetBlockIds(blockNum);
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation cancelled as IsCancellationRequested flag is true while uploading blocks in File Share Service with CancellationToken:{cancellationToken.Source} for BlockId:{BlockId} and file:{FileName} and BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", JsonConvert.SerializeObject(cancellationTokenSource.Token), blockId, customFileInfo.Name, batchId, correlationId);
                    throw new OperationCanceledException();
                }
                var blockUploadMetaData = new UploadBlockMetaData()
                {
                    BatchId = batchId,
                    BlockId = blockId,
                    FullFileName = customFileInfo.FullName,
                    JwtToken = accessToken,
                    Offset = uploadedBytes,
                    Length = readBlockSize,
                    FileName = customFileInfo.Name
                };
                ParallelBlockUploadTasks.Add(UploadFileBlockMetaData(blockUploadMetaData, correlationId, cancellationTokenSource, cancellationToken));
                blockIdList.Add(blockId);
                uploadedBytes += readBlockSize;
                //run uploads in parallel	
                if (ParallelBlockUploadTasks.Count >= fileShareServiceConfig.Value.ParallelUploadThreadCount)
                {
                    Task.WaitAll(ParallelBlockUploadTasks.ToArray());
                    ParallelBlockUploadTasks.Clear();
                }
            }
            if (ParallelBlockUploadTasks.Count > 0)
            {
                await Task.WhenAll(ParallelBlockUploadTasks);
                ParallelBlockUploadTasks.Clear();
            }
            return blockIdList;
        }

        private Task UploadFileBlockMetaData(UploadBlockMetaData UploadBlockMetaData, string correlationId, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken)
        {
            return logger.LogStartEndAndElapsedTime(EventIds.UploadFileBlockStarted, EventIds.UploadFileBlockCompleted,
                    "UploadFileBlock for BlockId:{BlockId} and file:{FileName} and Batch:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                    async () =>
                    {
                        var byteData = fileSystemHelper.UploadFileBlockMetaData(UploadBlockMetaData);
                        var blockMd5Hash = CommonHelper.CalculateMD5(byteData);
                        HttpResponseMessage httpResponse;
                        httpResponse = await fileShareServiceClient.UploadFileBlockAsync(HttpMethod.Put, fileShareServiceConfig.Value.BaseUrl, UploadBlockMetaData.BatchId, UploadBlockMetaData.FileName, UploadBlockMetaData.BlockId, byteData, blockMd5Hash, UploadBlockMetaData.JwtToken, cancellationToken, correlationId);

                        if (!httpResponse.IsSuccessStatusCode)
                        {
                            cancellationTokenSource.Cancel();
                            logger.LogError(EventIds.UploadFileBlockNonOkResponse.ToEventId(), "Error in uploading file blocks with uri {RequestUri} responded with {StatusCode} for BlockId:{BlockId} and file:{FileName} and BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, UploadBlockMetaData.BlockId, UploadBlockMetaData.FileName, UploadBlockMetaData.BatchId, correlationId);
                            logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Request cancelled for Error in uploading file blocks with CancellationToken:{ cancellationTokenSource.Token} with uri {RequestUri} responded with {StatusCode} for BlockId:{BlockId} and file:{FileName} and BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", JsonConvert.SerializeObject(cancellationTokenSource.Token), httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, UploadBlockMetaData.BlockId, UploadBlockMetaData.FileName, UploadBlockMetaData.BatchId, correlationId);
                            throw new FulfilmentException(EventIds.UploadFileBlockNonOkResponse.ToEventId());
                        }
                    }, UploadBlockMetaData.BlockId, UploadBlockMetaData.FileName, UploadBlockMetaData.BatchId, correlationId);
        }

        private Task<bool> WriteBlockFile(WriteBlocksToFileMetaData writeBlocksToFileMetaData, string correlationId)
        {
            return logger.LogStartEndAndElapsedTimeAsync(EventIds.WriteBlocksToFileStart, EventIds.WriteBlockToFileNonOkResponse,
                    "Write Blocks to file:{FileName} for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                    async () =>
                    {
                        var writeBlockfileModel = new WriteBlockFileModel()
                        {
                            BlockIds = writeBlocksToFileMetaData.BlockIds
                        };
                        HttpResponseMessage httpResponse;
                        httpResponse = await fileShareServiceClient.WriteBlockInFileAsync(HttpMethod.Put, fileShareServiceConfig.Value.BaseUrl, writeBlocksToFileMetaData.BatchId, writeBlocksToFileMetaData.FileName, writeBlockfileModel, writeBlocksToFileMetaData.AccessToken, correlationId: correlationId);
                        if (!httpResponse.IsSuccessStatusCode)
                        {
                            logger.LogError(EventIds.WriteBlockToFileNonOkResponse.ToEventId(), "Error in writing Blocks with uri:{RequestUri} responded with {StatusCode} for file:{FileName} and BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, writeBlocksToFileMetaData.FileName, writeBlocksToFileMetaData.BatchId, correlationId);
                            throw new FulfilmentException(EventIds.WriteBlockToFileNonOkResponse.ToEventId());
                        }
                        return true;
                    }, writeBlocksToFileMetaData.FileName, writeBlocksToFileMetaData.BatchId, correlationId);
        }


    }
}
