// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.Helpers.FileShare.FileShareInterfaces
{
    public interface IFileShareBatchService
    {
        Task<CreateBatchResponse> CreateBatch(string userOid, string correlationId);
        Task<bool> CommitBatchToFss(string batchId, string correlationId, string exchangeSetZipPath, string fileName = "zip");
        Task<SearchBatchResponse> GetBatchInfoBasedOnProducts(List<Products> products, SalesCatalogueServiceResponseQueueMessage message, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken, string exchangeSetRootPath, string businessUnit);
    }
}
