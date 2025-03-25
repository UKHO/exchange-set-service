// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IFileShareUploadService
    {
        Task<bool> UploadFileToFileShareService(string batchId, string exchangeSetZipRootPath, string correlationId, string fileName);
        Task<bool> UploadLargeMediaFileToFileShareService(string batchId, string exchangeSetZipPath, string correlationId, string fileName);
    }
}
