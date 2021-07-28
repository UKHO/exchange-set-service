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

        public async Task<bool> SendCallBackReponse(SalesCatalogueProductResponse salesCatalogueProductResponse, SalesCatalogueServiceResponseQueueMessage scsResponseQueueMessage)
        {
            if (scsResponseQueueMessage.CallbackUri != "")
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
                    Specversion = essCallBackConfiguration.Value.SpecVersion,
                    Type = essCallBackConfiguration.Value.Type,
                    Source = essCallBackConfiguration.Value.Source,
                    Id = Guid.NewGuid().ToString(),
                    Time = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                    Subject = essCallBackConfiguration.Value.Subject,
                    DataContentType = "application/json",
                    Data = exchangeSetResponse
                };

                string payloadJson = JsonConvert.SerializeObject(callBackResponse);

                logger.LogInformation(EventIds.ESSPostCallBackRequestStart.ToEventId(), "Post callback request started for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", scsResponseQueueMessage.BatchId, scsResponseQueueMessage.CorrelationId);
                
                await callBackClient.CallBackApi(HttpMethod.Post, payloadJson, scsResponseQueueMessage.CallbackUri);
                
                logger.LogInformation(EventIds.ESSPostCallBackRequestCompleted.ToEventId(), "Post callback request completed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", scsResponseQueueMessage.BatchId, scsResponseQueueMessage.CorrelationId);
                return true;
            }
            else 
            {
                logger.LogInformation(EventIds.ESSPostCallBackRequestNotSend.ToEventId(), "Post callback request not send for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", scsResponseQueueMessage.BatchId, scsResponseQueueMessage.CorrelationId);
                return false;
            }
        }

        public List<RequestedProductsNotInExchangeSet> GetRequestedProductsNotInExchangeSet(SalesCatalogueProductResponse salesCatalogueProductResponse)
        {
            var listRequestedProductsNotInExchangeSet = new List<RequestedProductsNotInExchangeSet>();
            foreach (var item in salesCatalogueProductResponse.ProductCounts.RequestedProductsNotReturned)
            {
                var requestedProductsNotInExchangeSet = new RequestedProductsNotInExchangeSet();
                requestedProductsNotInExchangeSet.ProductName = item.ProductName;
                requestedProductsNotInExchangeSet.Reason = item.Reason;
                listRequestedProductsNotInExchangeSet.Add(requestedProductsNotInExchangeSet);
            }
            return listRequestedProductsNotInExchangeSet;
        }
    }
}
