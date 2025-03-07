// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers.Auth;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public class FileShareSearchService(
        ILogger<FileShareService> logger,
        IAuthFssTokenProvider authFssTokenProvider,
        IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
        IFileShareServiceClient fileShareServiceClient)
         : IFileShareSearchService
    {
        public async Task<string> SearchReadMeFilePath(string batchId, string correlationId)
        {
            var payloadJson = string.Empty;
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            var filePath = string.Empty;
            var uri = $"{fileShareServiceConfig.Value.BaseUrl}/batch?$filter={fileShareServiceConfig.Value.ProductType} fileName eq '{fileShareServiceConfig.Value.ReadMeFileName}' and BusinessUnit eq '{fileShareServiceConfig.Value.S63BusinessUnit}'";
            HttpResponseMessage httpResponse;
            httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, uri, CancellationToken.None, correlationId);
            if (httpResponse.IsSuccessStatusCode)
            {
                var searchBatchResponse = await SearchBatch(httpResponse);
                if (searchBatchResponse.Entries.Count > 0)
                {
                    var batchResult = searchBatchResponse.Entries.FirstOrDefault();
                    filePath = batchResult.Files.FirstOrDefault()?.Links.Get.Href;
                }
                else
                {
                    logger.LogError(EventIds.ReadMeTextFileNotFound.ToEventId(), "Error in file share service readme.txt not found for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                    throw new FulfilmentException(EventIds.ReadMeTextFileNotFound.ToEventId());
                }
            }
            else
            {
                logger.LogError(EventIds.QueryFileShareServiceReadMeFileNonOkResponse.ToEventId(), "Error in file share service while searching ReadMe file with uri {RequestUri} responded with {StatusCode} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, batchId, correlationId);
                throw new FulfilmentException(EventIds.QueryFileShareServiceReadMeFileNonOkResponse.ToEventId());
            }

            return filePath;
        }

        public async Task<string> SearchIhoCrtFilePath(string batchId, string correlationId)
        {
            var payloadJson = string.Empty;
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            var filePath = string.Empty;
            var uri = $"{fileShareServiceConfig.Value.BaseUrl}/batch?$filter={fileShareServiceConfig.Value.ProductType} fileName eq '{fileShareServiceConfig.Value.IhoCrtFileName}' and BusinessUnit eq '{fileShareServiceConfig.Value.S63BusinessUnit}'";
            HttpResponseMessage httpResponse;
            httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, uri, CancellationToken.None, correlationId);
            if (httpResponse.IsSuccessStatusCode)
            {
                var searchBatchResponse = await SearchBatch(httpResponse);
                if (searchBatchResponse.Entries.Count > 0)
                {
                    var batchResult = searchBatchResponse.Entries.FirstOrDefault();
                    filePath = batchResult.Files.FirstOrDefault()?.Links.Get.Href;
                }
                else
                {
                    logger.LogError(EventIds.IhoCrtFileNotFound.ToEventId(), "Error in file share service IHO.crt not found for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                    throw new FulfilmentException(EventIds.IhoCrtFileNotFound.ToEventId());
                }
            }
            else
            {
                logger.LogError(EventIds.QueryFileShareServiceIhoCrtFileNonOkResponse.ToEventId(), "Error in file share service while searching IHO.crt file with uri {RequestUri} responded with {StatusCode} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, batchId, correlationId);
                throw new FulfilmentException(EventIds.QueryFileShareServiceIhoCrtFileNonOkResponse.ToEventId());
            }

            return filePath;
        }

        public async Task<string> SearchIhoPubFilePath(string batchId, string correlationId)
        {
            var payloadJson = string.Empty;
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            var filePath = string.Empty;
            var uri = $"{fileShareServiceConfig.Value.BaseUrl}/batch?$filter={fileShareServiceConfig.Value.ProductType} fileName eq '{fileShareServiceConfig.Value.IhoPubFileName}' and BusinessUnit eq '{fileShareServiceConfig.Value.S63BusinessUnit}'";
            HttpResponseMessage httpResponse;
            httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, uri, CancellationToken.None, correlationId);
            if (httpResponse.IsSuccessStatusCode)
            {
                var searchBatchResponse = await SearchBatch(httpResponse);
                if (searchBatchResponse.Entries.Count > 0)
                {
                    var batchResult = searchBatchResponse.Entries.FirstOrDefault();
                    filePath = batchResult.Files.FirstOrDefault()?.Links.Get.Href;
                }
                else
                {
                    logger.LogError(EventIds.IhoPubFileNotFound.ToEventId(), "Error in file share service IHO.pub not found for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                    throw new FulfilmentException(EventIds.IhoPubFileNotFound.ToEventId());
                }
            }
            else
            {
                logger.LogError(EventIds.QueryFileShareServiceIhoPubFileNonOkResponse.ToEventId(), "Error in file share service while searching IHO.pub file with uri {RequestUri} responded with {StatusCode} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, batchId, correlationId);
                throw new FulfilmentException(EventIds.QueryFileShareServiceIhoPubFileNonOkResponse.ToEventId());
            }

            return filePath;
        }


        // This function is used to search Info and Adc folder details from FSS for large exchange set
        public async Task<IEnumerable<BatchFile>> SearchFolderDetails(string batchId, string correlationId, string uri)
        {
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);

            var httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, null, accessToken, uri, CancellationToken.None, correlationId);

            IEnumerable<BatchFile> fileDetails = null;
            if (httpResponse.IsSuccessStatusCode)
            {
                var searchBatchResponse = await SearchBatch(httpResponse);
                if (searchBatchResponse.Entries.Count > 0)
                {
                    var batchResult = searchBatchResponse.Entries.OrderByDescending(j => j.BatchPublishedDate).FirstOrDefault();
                    fileDetails = batchResult?.Files.Select(x => new BatchFile
                    {
                        Filename = x.Filename,
                        Links = new Links
                        {
                            Get = new Link
                            {
                                Href = x.Links.Get.Href
                            }
                        }
                    });
                }
                else
                {
                    logger.LogError(EventIds.SearchFolderFilesNotFound.ToEventId(), "Error in file share service, folder files not found for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                    throw new FulfilmentException(EventIds.SearchFolderFilesNotFound.ToEventId());
                }
                logger.LogInformation(EventIds.QueryFileShareServiceSearchFolderFileOkResponse.ToEventId(), "Successfully searched files path for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
            }
            else
            {
                logger.LogError(EventIds.QueryFileShareServiceSearchFolderFileNonOkResponse.ToEventId(), "Error in file share service while searching folder files with uri {RequestUri} responded with {StatusCode} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, batchId, correlationId);
                throw new FulfilmentException(EventIds.QueryFileShareServiceSearchFolderFileNonOkResponse.ToEventId());
            }
            return fileDetails;
        }

        private async Task<SearchBatchResponse> SearchBatch(HttpResponseMessage httpResponse)
        {
            var body = await httpResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<SearchBatchResponse>(body);
        }
    }
}
