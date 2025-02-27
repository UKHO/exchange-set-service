// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IFileShareDownloadService
    {
        Task<bool> DownloadBatchFiles(BatchDetail entry, IEnumerable<string> uri, string downloadPath, SalesCatalogueServiceResponseQueueMessage queueMessage, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken);
        Task<bool> DownloadReadMeFileFromFssAsync(string readMeFilePath, string batchId, string exchangeSetRootPath, string correlationId);
        Task<bool> DownloadFolderDetails(string batchId, string correlationId, IEnumerable<BatchFile> fileDetails, string exchangeSetPath);
        Task<bool> DownloadIhoCrtFile(string ihoCrtFilePath, string batchId, string aioExchangeSetPath, string correlationId);
        Task<bool> DownloadIhoPubFile(string ihoPubFilePath, string batchId, string aioExchangeSetPath, string correlationId);
        Task<bool> DownloadReadMeFileFromCacheAsync(string batchId, string exchangeSetRootPath, string correlationId);
    }
}
