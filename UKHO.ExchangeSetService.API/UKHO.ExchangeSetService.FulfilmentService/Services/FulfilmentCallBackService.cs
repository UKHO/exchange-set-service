using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        private readonly AioConfiguration aioConfiguration;

        public FulfilmentCallBackService(IOptions<EssCallBackConfiguration> essCallBackConfiguration,
                                         ICallBackClient callBackClient,
                                         IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
                                         ILogger<FulfilmentCallBackService> logger,
                                         IOptions<AioConfiguration> aioConfiguration)
        {
            this.essCallBackConfiguration = essCallBackConfiguration;
            this.callBackClient = callBackClient;
            this.fileShareServiceConfig = fileShareServiceConfig;
            this.logger = logger;
            this.aioConfiguration = aioConfiguration.Value;
        }

        public async Task<bool> SendCallBackResponse(SalesCatalogueProductResponse salesCatalogueProductResponse, SalesCatalogueServiceResponseQueueMessage scsResponseQueueMessage)
        {
            if (!string.IsNullOrWhiteSpace(scsResponseQueueMessage.CallbackUri))
            {
                try
                {
                    ExchangeSetResponse exchangeSetResponse = SetExchangeSetResponse(salesCatalogueProductResponse, scsResponseQueueMessage);

                    CallBackResponse callBackResponse = SetCallBackResponse(exchangeSetResponse);
                    callBackResponse.Subject = essCallBackConfiguration.Value.Subject;

                    if (ValidateCallbackRequestPayload(callBackResponse))
                    {
                        string payloadJson = JsonConvert.SerializeObject(callBackResponse);

                        return await SendResponseToCallBackApi(false, payloadJson, scsResponseQueueMessage);
                    }
                    else
                    {
                        logger.LogError(EventIds.ExchangeSetCreatedPostCallbackUriNotCalled.ToEventId(), "Post Callback uri is not called after exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} as payload data is incorrect.", scsResponseQueueMessage.BatchId, scsResponseQueueMessage.CorrelationId);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(EventIds.ExchangeSetCreatedPostCallbackUriNotCalled.ToEventId(), ex, "Post Callback uri is not called after exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} and Exception:{Message}", scsResponseQueueMessage.BatchId, scsResponseQueueMessage.CorrelationId, ex.Message);
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
            salesCatalogueProductResponse.ProductCounts.RequestedProductsNotReturned = new List<RequestedProductsNotReturned> { new RequestedProductsNotReturned { ProductName = null, Reason = essCallBackConfiguration.Value.Reason } };

            if (!string.IsNullOrWhiteSpace(scsResponseQueueMessage.CallbackUri))
            {
                try
                {
                    ExchangeSetResponse exchangeSetResponse = SetExchangeSetResponse(salesCatalogueProductResponse, scsResponseQueueMessage);
                    exchangeSetResponse.Links.ExchangeSetFileUri = null;
                    exchangeSetResponse.Links.ExchangeSetErrorFileUri = new LinkSetErrorFileUri { Href = $"{fileShareServiceConfig.Value.PublicBaseUrl}/batch/{scsResponseQueueMessage.BatchId}/files/{fileShareServiceConfig.Value.ErrorFileName}" };

                    CallBackResponse callBackResponse = SetCallBackResponse(exchangeSetResponse);
                    callBackResponse.Subject = essCallBackConfiguration.Value.ErrorSubject;

                    if (ValidateCallbackErrorRequestPayload(callBackResponse))
                    {
                        string payloadJson = JsonConvert.SerializeObject(callBackResponse);

                        return await SendResponseToCallBackApi(true, payloadJson, scsResponseQueueMessage);
                    }
                    else
                    {
                        logger.LogError(EventIds.ExchangeSetCreatedWithErrorPostCallbackUriNotCalled.ToEventId(), "Post Callback uri is not called after exchange set is created with error for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} as payload data is incorrect.", scsResponseQueueMessage.BatchId, scsResponseQueueMessage.CorrelationId);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(EventIds.ExchangeSetCreatedWithErrorPostCallbackUriNotCalled.ToEventId(), "Post Callback uri is not called after exchange set is created with error for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} and Exception:{Message}", scsResponseQueueMessage.BatchId, scsResponseQueueMessage.CorrelationId, ex.Message);
                    return false;
                }
            }
            else
            {
                logger.LogInformation(EventIds.ExchangeSetCreatedWithErrorPostCallbackUriNotProvided.ToEventId(), "Post callback uri was not provided by requestor for exchange set creation with error for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", scsResponseQueueMessage.BatchId, scsResponseQueueMessage.CorrelationId);
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
            var payLoad = JsonConvert.SerializeObject(callBackResponse, Formatting.Indented);
            logger.LogInformation(EventIds.ValidateCallbackRequestPayloadStart.ToEventId(), 

                "Callback payload validation started for BatchId:{BatchId}, Payload: {Payload}", callBackResponse.Data.BatchId, payLoad);

            return (
                       callBackResponse.Data.RequestedProductCount >= 0 
                       || callBackResponse.Data.RequestedAioProductCount >= 0
                   )
                   && !string.IsNullOrWhiteSpace(callBackResponse.Data.Links.ExchangeSetBatchStatusUri.Href)
                   && !string.IsNullOrWhiteSpace(callBackResponse.Data.Links.ExchangeSetBatchDetailsUri.Href) 
                   && (
                        !string.IsNullOrWhiteSpace(callBackResponse.Data.Links.ExchangeSetFileUri?.Href)
                        || !string.IsNullOrWhiteSpace(callBackResponse.Data.Links.AioExchangeSetFileUri?.Href)
                       )
                   && !string.IsNullOrWhiteSpace(callBackResponse.Id);
        }

        public bool ValidateCallbackErrorRequestPayload(CallBackResponse callBackResponse)
        {
            return (callBackResponse.Data.RequestedProductCount >= 0 && !string.IsNullOrWhiteSpace(callBackResponse.Data.Links.ExchangeSetBatchStatusUri.Href) && !string.IsNullOrWhiteSpace(callBackResponse.Data.Links.ExchangeSetBatchDetailsUri.Href) && Links.Equals(callBackResponse.Data.Links.ExchangeSetFileUri, null) && !string.IsNullOrWhiteSpace(callBackResponse.Id) && int.Equals(callBackResponse.Data.ExchangeSetCellCount, 0));
        }

        public async Task<bool> SendResponseToCallBackApi(bool errorStatus, string payloadJson, SalesCatalogueServiceResponseQueueMessage scsResponseQueueMessage)
        {
            await callBackClient.CallBackApi(HttpMethod.Post, payloadJson, scsResponseQueueMessage.CallbackUri);

            if (!errorStatus)
                logger.LogInformation(EventIds.ExchangeSetCreatedPostCallbackUriCalled.ToEventId(), "Post Callback uri - {CallbackUri} is called with pay load {payloadJson} after exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", scsResponseQueueMessage.CallbackUri, payloadJson, scsResponseQueueMessage.BatchId, scsResponseQueueMessage.CorrelationId);
            else
                logger.LogInformation(EventIds.ExchangeSetCreatedWithErrorPostCallbackUriCalled.ToEventId(), "Post Callback uri is called after exchange set is created with error for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", scsResponseQueueMessage.BatchId, scsResponseQueueMessage.CorrelationId);

            return true;
        }

        public ExchangeSetResponse SetExchangeSetResponse(SalesCatalogueProductResponse salesCatalogueProductResponse, SalesCatalogueServiceResponseQueueMessage scsResponseQueueMessage)
        {
            var configAioCells = GetAioCells();

            var links = new Links()
            {
                ExchangeSetBatchStatusUri = new LinkSetBatchStatusUri
                {
                    Href =
                        $"{fileShareServiceConfig.Value.PublicBaseUrl}/batch/{scsResponseQueueMessage.BatchId}/status"
                },
                ExchangeSetBatchDetailsUri = new LinkSetBatchDetailsUri
                    { Href = $"{fileShareServiceConfig.Value.PublicBaseUrl}/batch/{scsResponseQueueMessage.BatchId}" },
                
            };

            var validAioCells = salesCatalogueProductResponse.Products
                .Where(x => configAioCells.Any(y => y.Equals(x.ProductName)))
                .Select(x => x.ProductName)
                .ToList();

            var validEncCells = salesCatalogueProductResponse.Products
                .Where(x => !configAioCells.Any(y => y.Equals(x.ProductName)))
                .Select(x => x.ProductName).ToList();

            var exchangeSetResponse = new ExchangeSetResponse()
            {
                ExchangeSetUrlExpiryDateTime = Convert.ToDateTime(scsResponseQueueMessage.ExchangeSetUrlExpiryDate).ToUniversalTime(),
                RequestedProductCount = salesCatalogueProductResponse.ProductCounts.RequestedProductCount.Value,
                ExchangeSetCellCount = salesCatalogueProductResponse.ProductCounts.ReturnedProductCount.Value,
                RequestedProductsAlreadyUpToDateCount = salesCatalogueProductResponse.ProductCounts.RequestedProductsAlreadyUpToDateCount.Value,
                RequestedProductsNotInExchangeSet = GetRequestedProductsNotInExchangeSet(salesCatalogueProductResponse),
                BatchId = scsResponseQueueMessage.BatchId
            };

            if (aioConfiguration.IsAioEnabled)
            {
                exchangeSetResponse.RequestedAioProductCount = scsResponseQueueMessage.RequestedAioProductCount;
                exchangeSetResponse.RequestedProductCount = scsResponseQueueMessage.RequestedProductCount;
                exchangeSetResponse.ExchangeSetCellCount -= validAioCells.Count;
                exchangeSetResponse.AioExchangeSetCellCount = validAioCells.Count;

                exchangeSetResponse.RequestedAioProductsAlreadyUpToDateCount = scsResponseQueueMessage.RequestedAioProductsAlreadyUpToDateCount;
                exchangeSetResponse.RequestedProductsAlreadyUpToDateCount = scsResponseQueueMessage.RequestedProductsAlreadyUpToDateCount;
                
                if (validAioCells.Count > 0 || scsResponseQueueMessage.IsEmptyAioExchangeSet)
                {
                    links.AioExchangeSetFileUri = new LinkSetFileUri
                    {
                        Href =
                            $"{fileShareServiceConfig.Value.PublicBaseUrl}/batch/{scsResponseQueueMessage.BatchId}/files/{fileShareServiceConfig.Value.AioExchangeSetFileName}"
                    };
                }
            }
            else
            {
                exchangeSetResponse.RequestedProductsNotInExchangeSet.AddRange(validAioCells.Select(x => new RequestedProductsNotInExchangeSet
                {
                    ProductName = x,
                    Reason = "invalidProduct"
                }));
            }

            if (validEncCells.Count > 0 || !aioConfiguration.IsAioEnabled || scsResponseQueueMessage.IsEmptyEncExchangeSet)
            {
                links.ExchangeSetFileUri = new LinkSetFileUri
                {
                    Href =
                        $"{fileShareServiceConfig.Value.PublicBaseUrl}/batch/{scsResponseQueueMessage.BatchId}/files/{fileShareServiceConfig.Value.ExchangeSetFileName}"
                };
            }

            exchangeSetResponse.Links = links;

            return exchangeSetResponse;
        }
        
        private IEnumerable<string> GetAioCells()
        {
            return !string.IsNullOrEmpty(aioConfiguration.AioCells) ? new(aioConfiguration.AioCells.Split(',').Select(s => s.Trim())) : new List<string>();
        }

        public CallBackResponse SetCallBackResponse(ExchangeSetResponse exchangeSetResponse)
        {
            return new CallBackResponse()
            {
                SpecVersion = essCallBackConfiguration.Value.SpecVersion,
                Type = essCallBackConfiguration.Value.Type,
                Source = essCallBackConfiguration.Value.Source,
                Id = Guid.NewGuid().ToString(),
                Time = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                Subject = string.Empty,
                DataContentType = "application/json",
                Data = exchangeSetResponse
            };
        }
    }
}