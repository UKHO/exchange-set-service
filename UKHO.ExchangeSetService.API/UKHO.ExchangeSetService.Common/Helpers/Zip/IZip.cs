// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers.Zip
{
    public interface IZip
    {
        Task<bool> CreateZipFileForExchangeSet(string batchId, string exchangeSetZipRootPath, string correlationId);
    }
}
