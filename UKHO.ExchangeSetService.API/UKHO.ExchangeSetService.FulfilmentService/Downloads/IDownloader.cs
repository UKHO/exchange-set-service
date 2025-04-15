// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.FulfilmentService.Downloads
{
    public interface IDownloader
    {
        Task DownloadInfoFolderFiles(string batchId, string exchangeSetInfoPath, string correlationId);
        Task DownloadAdcFolderFiles(string batchId, string exchangeSetAdcPath, string correlationId);
        Task<bool> DownloadReadMeFileAsync(string batchId, string exchangeSetRootPath, string correlationId);
        Task<bool> DownloadReadMeFileFromFssAsync(string batchId, string exchangeSetRootPath, string correlationId);
        Task<bool> DownloadIhoCrtFile(string batchId, string aioExchangeSetPath, string correlationId);
        Task<bool> DownloadIhoPubFile(string batchId, string exchangeSetRootPath, string correlationId);
        Task DownloadLargeMediaReadMeFile(string batchId, string exchangeSetPath, string correlationId);
    }
}
