// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using UKHO.ExchangeSetService.Common.Helpers.Zip;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IFileShareService : IFileShareBatchService, IFileShareDownloadService, IFileShareSearchService, IFileShareUploadService, IZip
    {
    }
}
