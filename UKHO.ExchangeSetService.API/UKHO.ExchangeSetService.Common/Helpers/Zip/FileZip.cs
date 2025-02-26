// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Logging;

namespace UKHO.ExchangeSetService.Common.Helpers.Zip
{
    public class FileZip(ILogger<FileZip> logger, IFileSystemHelper fileSystemHelper, IOptions<FileShareServiceConfiguration> fileShareServiceConfig)
    {
        public async Task<bool> CreateZipFileForExchangeSet(string batchId, string exchangeSetZipRootPath, string correlationId)
        {
            var isCreateZipFileExchangeSetCreated = false;
            var zipName = $"{exchangeSetZipRootPath}.zip";
            var filePath = Path.Combine(exchangeSetZipRootPath, zipName);
            if (fileSystemHelper.CheckDirectoryAndFileExists(exchangeSetZipRootPath, filePath))
            {
                fileSystemHelper.CreateZipFile(exchangeSetZipRootPath, zipName);
                await Task.CompletedTask;

                if (fileSystemHelper.CheckFileExists(zipName))
                {
                    logger.LogInformation(EventIds.CreateZipFileRequestCompleted.ToEventId(), "Exchange set zip:{ExchangeSetFileName} created for BatchId:{BatchId} and  _X-Correlation-ID:{correlationId}", fileSystemHelper.GetFileName(zipName), batchId, correlationId);
                    isCreateZipFileExchangeSetCreated = true;
                }
                else
                {
                    logger.LogError(EventIds.ErrorInCreatingZipFile.ToEventId(), "Error in creating exchange set zip:{ExchangeSetFileName} for BatchId:{BatchId} and _X-Correlation-ID:{correlationId}", fileSystemHelper.GetFileName(zipName), batchId, correlationId);
                    throw new FulfilmentException(EventIds.ErrorInCreatingZipFile.ToEventId());
                }
            }
            else
            {
                logger.LogError(EventIds.ErrorInCreatingZipFile.ToEventId(), "Error in creating exchange set zip:{ExchangeSetFileName} for BatchId:{BatchId} and _X-Correlation-ID:{correlationId}", fileShareServiceConfig.Value.ExchangeSetFileName, batchId, correlationId);
                throw new FulfilmentException(EventIds.ErrorInCreatingZipFile.ToEventId());
            }
            return isCreateZipFileExchangeSetCreated;
        }
    }
}
