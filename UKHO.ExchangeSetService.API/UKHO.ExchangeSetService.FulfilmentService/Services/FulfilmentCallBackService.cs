using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
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

        public FulfilmentCallBackService(IOptions<EssCallBackConfiguration> essCallBackConfiguration,
                                         ICallBackClient callBackClient,
                                         IOptions<FileShareServiceConfiguration> fileShareServiceConfig)
        {
            this.essCallBackConfiguration = essCallBackConfiguration;
            this.callBackClient = callBackClient;
            this.fileShareServiceConfig = fileShareServiceConfig;
        }

        public async Task<bool> SendCallBackReponse(SalesCatalogueProductResponse salesCatalogueProductResponse, SalesCatalogueServiceResponseQueueMessage scsResponseQueueMessage)
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
            payloadJson = "<QueueMessage><MessageText>" + payloadJson + "</MessageText></QueueMessage>";

            await callBackClient.CallBackApi(HttpMethod.Post, payloadJson, scsResponseQueueMessage.CallbackUri, scsResponseQueueMessage.CorrelationId);

            return true;
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
