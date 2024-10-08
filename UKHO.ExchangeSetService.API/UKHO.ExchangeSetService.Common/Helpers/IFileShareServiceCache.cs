﻿using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.AzureTableEntities;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IFileShareServiceCache
    {
        Task<List<Products>> GetNonCachedProductDataForFss(List<Products> products, SearchBatchResponse internalSearchBatchResponse, string exchangeSetRootPath, SalesCatalogueServiceResponseQueueMessage queueMessage, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken, string businessUnit);
        Task CopyFileToBlob(Stream stream, string fileName, string batchId);
        Task InsertOrMergeFssCacheDetail(FssSearchResponseCache fssSearchResponseCache);
        Task<Stream> DownloadFileFromCacheAsync(string fileName, string containerName);
    }
}
