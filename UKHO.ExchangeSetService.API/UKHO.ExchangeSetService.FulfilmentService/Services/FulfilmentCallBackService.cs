using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.FulfilmentService.Configuration;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public class FulfilmentCallBackService : IFulfilmentCallBackService
    {
        private readonly IOptions<EssCallBackConfiguration> essCallBackConfiguration;
        private readonly ICallBackClient callBackClient;
        private readonly IOptions<FileShareServiceConfiguration> fileShareServiceConfig;
        private readonly ILogger<FulfilmentCallBackService> logger;

        public FulfilmentCallBackService(IOptions<EssCallBackConfiguration> essCallBackConfiguration,
                                         ICallBackClient callBackClient,
                                         IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
                                         ILogger<FulfilmentCallBackService> logger)
        {
            this.essCallBackConfiguration = essCallBackConfiguration;
            this.callBackClient = callBackClient;
            this.fileShareServiceConfig = fileShareServiceConfig;
            this.logger = logger;
        }

        public async Task<bool> SendCallBackResponse(SalesCatalogueProductResponse salesCatalogueProductResponse, SalesCatalogueServiceResponseQueueMessage scsResponseQueueMessage)
        {
            if (!string.IsNullOrWhiteSpace(scsResponseQueueMessage.CallbackUri))
            {
                ExchangeSetResponse exchangeSetResponse = new ExchangeSetResponse()
                {
                    Links = new Links()
                    {
                        ExchangeSetBatchStatusUri = new LinkSetBatchStatusUri { Href = $"{fileShareServiceConfig.Value.BaseUrl}/batch/{scsResponseQueueMessage.BatchId}" },
                        ExchangeSetFileUri = new LinkSetFileUri { Href = $"{fileShareServiceConfig.Value.BaseUrl}/batch/{scsResponseQueueMessage.BatchId}/files/{fileShareServiceConfig.Value.ExchangeSetFileName}" }
                    },
                    ExchangeSetUrlExpiryDateTime = Convert.ToDateTime(scsResponseQueueMessage.ExchangeSetUrlExpiryDate).ToUniversalTime(),
                    RequestedProductCount = salesCatalogueProductResponse.ProductCounts.RequestedProductCount.Value,
                    ExchangeSetCellCount = salesCatalogueProductResponse.ProductCounts.ReturnedProductCount.Value,
                    RequestedProductsAlreadyUpToDateCount = salesCatalogueProductResponse.ProductCounts.RequestedProductsAlreadyUpToDateCount.Value,
                    RequestedProductsNotInExchangeSet = GetRequestedProductsNotInExchangeSet(salesCatalogueProductResponse)
                };

                CallBackResponse callBackResponse = new CallBackResponse()
                {
                    SpecVersion = essCallBackConfiguration.Value.SpecVersion,
                    Type = essCallBackConfiguration.Value.Type,
                    Source = essCallBackConfiguration.Value.Source,
                    Id = Guid.NewGuid().ToString(),
                    Time = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                    Subject = essCallBackConfiguration.Value.Subject,
                    DataContentType = "application/json",
                    Data = exchangeSetResponse
                };

                if (ValidateCallbackRequestPayload(callBackResponse))
                {
                    string payloadJson = JsonConvert.SerializeObject(callBackResponse);

                    await callBackClient.CallBackApi(HttpMethod.Post, payloadJson, scsResponseQueueMessage.CallbackUri);

                    logger.LogInformation(EventIds.ExchangeSetCreatedPostCallbackUriCalled.ToEventId(), "Post Callback uri is called after exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", scsResponseQueueMessage.BatchId, scsResponseQueueMessage.CorrelationId);
                    return true;
                }
                else
                {
                    logger.LogError(EventIds.ExchangeSetCreatedPostCallbackUriNotCalled.ToEventId(), "Post Callback uri is not called after exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} as payload data is incorrect.", scsResponseQueueMessage.BatchId, scsResponseQueueMessage.CorrelationId);
                    return false;
                }
            }
            else 
            {
                logger.LogInformation(EventIds.ExchangeSetCreatedPostCallbackUriNotProvided.ToEventId(), "Post callback uri was not provided by requestor for successful exchange set creation for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", scsResponseQueueMessage.BatchId, scsResponseQueueMessage.CorrelationId);
                return false;
            }
        }

        public async Task<bool> SendCallBackErrorResponse(SalesCatalogueProductResponse salesCatalogueProductResponse, SalesCatalogueServiceResponseQueueMessage scsResponseQueueMessage)
        {
            salesCatalogueProductResponse.ProductCounts.ReturnedProductCount = 0;
            salesCatalogueProductResponse.ProductCounts.RequestedProductsNotReturned = new List<RequestedProductsNotReturned> {
                                new RequestedProductsNotReturned { ProductName = null, Reason = essCallBackConfiguration.Value.Reason }};

            if (!string.IsNullOrWhiteSpace(scsResponseQueueMessage.CallbackUri))
            {
                ExchangeSetErrorResponse exchangeSetErrorResponse = new ExchangeSetErrorResponse()
                {
                    Links = new ErrorLinks()
                    {
                        ExchangeSetBatchStatusUri = new LinkSetBatchStatusUri { Href = $"{fileShareServiceConfig.Value.BaseUrl}/batch/{scsResponseQueueMessage.BatchId}" },
                        ExchangeSetFileUri = null,
                        ExchangeSetErrorFileUri = new LinkSetErrorFileUri { Href = $"{fileShareServiceConfig.Value.BaseUrl}/batch/{scsResponseQueueMessage.BatchId}/files/{fileShareServiceConfig.Value.ErrorFileName}" }
                    },
                    ExchangeSetUrlExpiryDateTime = Convert.ToDateTime(scsResponseQueueMessage.ExchangeSetUrlExpiryDate).ToUniversalTime(),
                    RequestedProductCount = salesCatalogueProductResponse.ProductCounts.RequestedProductCount.Value,
                    ExchangeSetCellCount = salesCatalogueProductResponse.ProductCounts.ReturnedProductCount.Value,
                    RequestedProductsAlreadyUpToDateCount = salesCatalogueProductResponse.ProductCounts.RequestedProductsAlreadyUpToDateCount.Value,
                    RequestedProductsNotInExchangeSet = GetRequestedProductsNotInExchangeSet(salesCatalogueProductResponse)
                };

                CallBackErrorResponse callBackErrorResponse = new CallBackErrorResponse()
                {
                    SpecVersion = essCallBackConfiguration.Value.SpecVersion,
                    Type = essCallBackConfiguration.Value.Type,
                    Source = essCallBackConfiguration.Value.Source,
                    Id = Guid.NewGuid().ToString(),
                    Time = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                    Subject = essCallBackConfiguration.Value.ErrorSubject,
                    DataContentType = "application/json",
                    Data = exchangeSetErrorResponse
                };

                if (ValidateCallbackErrorRequestPayload(callBackErrorResponse))
                {
                    string payloadJson = JsonConvert.SerializeObject(callBackErrorResponse);

                    await callBackClient.CallBackApi(HttpMethod.Post, payloadJson, scsResponseQueueMessage.CallbackUri);

                    logger.LogInformation(EventIds.ExchangeSetErrorPostCallbackUriCalled.ToEventId(), "Post Callback uri is called after exchange set is not created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", scsResponseQueueMessage.BatchId, scsResponseQueueMessage.CorrelationId);
                    return true;
                }
                else
                {
                    logger.LogError(EventIds.ExchangeSetErrorPostCallbackUriNotCalled.ToEventId(), "Post Callback uri is not called after exchange set is not created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} as payload data is incorrect.", scsResponseQueueMessage.BatchId, scsResponseQueueMessage.CorrelationId);
                    return false;
                }
            }
            else
            {
                logger.LogInformation(EventIds.ExchangeSetErrorPostCallbackUriNotProvided.ToEventId(), "Post callback uri was not provided by requestor for unsuccessful exchange set creation for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", scsResponseQueueMessage.BatchId, scsResponseQueueMessage.CorrelationId);
                return false;
            }
        }

        public List<RequestedProductsNotInExchangeSet> GetRequestedProductsNotInExchangeSet(SalesCatalogueProductResponse salesCatalogueProductResponse)
        {
            var listRequestedProductsNotInExchangeSet = new List<RequestedProductsNotInExchangeSet>();
            foreach (var item in salesCatalogueProductResponse.ProductCounts.RequestedProductsNotReturned)
            {
                var requestedProductsNotInExchangeSet = new RequestedProductsNotInExchangeSet
                {
                    ProductName = item.ProductName,
                    Reason = item.Reason
                };
                listRequestedProductsNotInExchangeSet.Add(requestedProductsNotInExchangeSet);
            }
            return listRequestedProductsNotInExchangeSet;
        }

        public bool ValidateCallbackRequestPayload(CallBackResponse callBackResponse)
        {
         return (callBackResponse.Data.RequestedProductCount > 0 && !string.IsNullOrWhiteSpace(callBackResponse.Data.Links.ExchangeSetBatchStatusUri.Href) && !string.IsNullOrWhiteSpace(callBackResponse.Data.Links.ExchangeSetFileUri.Href) && !string.IsNullOrWhiteSpace(callBackResponse.Id));
        }

        public bool ValidateCallbackErrorRequestPayload(CallBackErrorResponse callBackResponse)
        {
            return (callBackResponse.Data.RequestedProductCount > 0 && !string.IsNullOrWhiteSpace(callBackResponse.Data.Links.ExchangeSetBatchStatusUri.Href) && Links.Equals(callBackResponse.Data.Links.ExchangeSetFileUri,null) && !string.IsNullOrWhiteSpace(callBackResponse.Id) && int.Equals(callBackResponse.Data.ExchangeSetCellCount,0));
        }
    }
}