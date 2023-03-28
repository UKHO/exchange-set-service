using AutoMapper;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Configuration;
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.AzureADB2C;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.API.Services
{
    public class ProductDataService : IProductDataService
    {
        private const string RFC1123Format = "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'";
        private readonly IProductIdentifierValidator productIdentifierValidator;
        private readonly IProductDataProductVersionsValidator productVersionsValidator;
        private readonly IProductDataSinceDateTimeValidator productDataSinceDateTimeValidator;
        private readonly ISalesCatalogueService salesCatalogueService;
        private readonly IMapper mapper;
        private readonly IFileShareService fileShareService;
        private readonly ILogger<ProductDataService> logger;
        private readonly IExchangeSetStorageProvider exchangeSetStorageProvider;
        private readonly IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageconfig;
        private readonly IMonitorHelper monitorHelper;
        private readonly UserIdentifier userIdentifier;
        private readonly IAzureAdB2CHelper azureAdB2CHelper;
        private readonly IOptions<AioConfiguration> aioConfiguration;

        public ProductDataService(IProductIdentifierValidator productIdentifierValidator,
            IProductDataProductVersionsValidator productVersionsValidator,
            IProductDataSinceDateTimeValidator productDataSinceDateTimeValidator,
            ISalesCatalogueService salesCatalougeService,
            IMapper mapper,
            IFileShareService fileShareService,
            ILogger<ProductDataService> logger, IExchangeSetStorageProvider exchangeSetStorageProvider,
            IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageconfig, IMonitorHelper monitorHelper,
            UserIdentifier userIdentifier, IAzureAdB2CHelper azureAdB2CHelper, IOptions<AioConfiguration> aioConfiguration)
        {
            this.productIdentifierValidator = productIdentifierValidator;
            this.productVersionsValidator = productVersionsValidator;
            this.productDataSinceDateTimeValidator = productDataSinceDateTimeValidator;
            this.salesCatalogueService = salesCatalougeService;
            this.mapper = mapper;
            this.fileShareService = fileShareService;
            this.logger = logger;
            this.exchangeSetStorageProvider = exchangeSetStorageProvider;
            this.essFulfilmentStorageconfig = essFulfilmentStorageconfig;
            this.monitorHelper = monitorHelper;
            this.userIdentifier = userIdentifier;
            this.azureAdB2CHelper = azureAdB2CHelper;
            this.aioConfiguration = aioConfiguration;
        }

        public async Task<ExchangeSetServiceResponse> CreateProductDataByProductIdentifiers(ProductIdentifierRequest productIdentifierRequest, AzureAdB2C azureAdB2C)
        {
            DateTime salesCatalogueServiceRequestStartedAt = DateTime.UtcNow;
            bool isAioEnabled = aioConfiguration.Value.AioEnabled.HasValue && aioConfiguration.Value.AioEnabled.Value;

            IEnumerable<string> aioCells = FilterAioCellsByProductIdentifiers(productIdentifierRequest);

            var salesCatalogueResponse = await salesCatalogueService.PostProductIdentifiersAsync(productIdentifierRequest.ProductIdentifier.ToList(), productIdentifierRequest.CorrelationId);

            if (!isAioEnabled && aioCells.Any())//when toggle off then add aio cells as invalidProduct
            {
                IEnumerable<RequestedProductsNotReturned> requestedProductsNotReturneds = aioCells.Select(x => new RequestedProductsNotReturned()
                {
                    ProductName = x,
                    Reason = "invalidProduct"
                });

                salesCatalogueResponse.ResponseBody.ProductCounts.RequestedProductsNotReturned.AddRange(requestedProductsNotReturneds);
                salesCatalogueResponse.ResponseBody.ProductCounts.RequestedProductCount += aioCells.Count();
            }

            long fileSize = 0;
            if (salesCatalogueResponse.ResponseCode == HttpStatusCode.OK)
            {
                fileSize = CommonHelper.GetFileSize(salesCatalogueResponse.ResponseBody);
                bool isAzureB2C = azureAdB2CHelper.IsAzureB2CUser(azureAdB2C, productIdentifierRequest.CorrelationId);
                if (isAzureB2C)
                {
                    var checkFileResponse = CheckIfExchangeSetTooLarge(fileSize);
                    if (checkFileResponse.HttpStatusCode != HttpStatusCode.OK)
                    {
                        return checkFileResponse;
                    }
                }
            }
            DateTime salesCatalogueServiceRequestCompletedAt = DateTime.UtcNow;
            monitorHelper.MonitorRequest("Sales Catalogue Service Product Identifier Request", salesCatalogueServiceRequestStartedAt, salesCatalogueServiceRequestCompletedAt, productIdentifierRequest.CorrelationId, null, null, fileSize, null);

            var response = SetExchangeSetResponse(salesCatalogueResponse, false);

            if (isAioEnabled)//when toggle on then add additional aio cell details
            {
                response.ExchangeSetResponse.RequestedAioProductCount = aioCells.Count();
                response.ExchangeSetResponse.AioExchangeSetCellCount = aioCells.Count();
                response.ExchangeSetResponse.RequestedAioProductsAlreadyUpToDateCount = 0;
            }

            if (response.HttpStatusCode != HttpStatusCode.OK && response.HttpStatusCode != HttpStatusCode.NotModified)
            {
                return response;
            }

            var exchangeSetServiceResponse = await SetExchangeSetResponseLinks(response, productIdentifierRequest.CorrelationId);

            if (exchangeSetServiceResponse.HttpStatusCode != HttpStatusCode.Created)
                return exchangeSetServiceResponse;

            string expiryDate = exchangeSetServiceResponse.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

            if (!string.IsNullOrEmpty(exchangeSetServiceResponse.BatchId))
            {
                await SaveSalesCatalogueStorageDetails(salesCatalogueResponse.ResponseBody, exchangeSetServiceResponse.BatchId, productIdentifierRequest.CallbackUri, productIdentifierRequest.CorrelationId, expiryDate, salesCatalogueResponse.ScsRequestDateTime);
            }
            return response;
        }

        public ExchangeSetServiceResponse CheckIfExchangeSetTooLarge(long fileSize)
        {
            var fileSizeInMB = CommonHelper.ConvertBytesToMegabytes(fileSize);
            if (fileSizeInMB >= essFulfilmentStorageconfig.Value.LargeExchangeSetSizeInMB)
            {
                var exchangeSetResponse = new ExchangeSetServiceResponse
                {
                    HttpStatusCode = HttpStatusCode.BadRequest,
                    IsExchangeSetTooLarge = true
                };
                return exchangeSetResponse;
            }
            else
            {
                var exchangeSetResponse = new ExchangeSetServiceResponse
                {
                    HttpStatusCode = HttpStatusCode.OK,
                    IsExchangeSetTooLarge = false
                };
                return exchangeSetResponse;
            }
        }

        public Task<ValidationResult> ValidateProductDataByProductIdentifiers(ProductIdentifierRequest productIdentifierRequest)
        {
            return productIdentifierValidator.Validate(productIdentifierRequest);
        }

        public async Task<ExchangeSetServiceResponse> CreateProductDataByProductVersions(ProductDataProductVersionsRequest request, AzureAdB2C azureAdB2C)
        {
            DateTime salesCatalogueServiceRequestStartedAt = DateTime.UtcNow;
            bool isAioEnabled = aioConfiguration.Value.AioEnabled.HasValue && aioConfiguration.Value.AioEnabled.Value;

            IEnumerable<string> aioCells = FilterAioCellsByProductVersions(request);

            var salesCatalogueResponse = await salesCatalogueService.PostProductVersionsAsync(request.ProductVersions, request.CorrelationId);

            if (!isAioEnabled && aioCells.Any())//when toggle off then add aio cells as invalidProduct
            {
                IEnumerable<RequestedProductsNotReturned> requestedProductsNotReturneds = aioCells.Select(x => new RequestedProductsNotReturned
                {
                    ProductName = x,
                    Reason = "invalidProduct"
                });

                salesCatalogueResponse.ResponseBody.ProductCounts.RequestedProductsNotReturned.AddRange(requestedProductsNotReturneds);
                salesCatalogueResponse.ResponseBody.ProductCounts.RequestedProductCount += aioCells.Count();
            }

            long fileSize = 0;
            if (salesCatalogueResponse.ResponseCode == HttpStatusCode.OK)
            {
                fileSize = CommonHelper.GetFileSize(salesCatalogueResponse.ResponseBody);
                bool isAzureB2C = azureAdB2CHelper.IsAzureB2CUser(azureAdB2C, request.CorrelationId);
                if (isAzureB2C)
                {
                    var checkFileResponse = CheckIfExchangeSetTooLarge(fileSize);
                    if (checkFileResponse.HttpStatusCode != HttpStatusCode.OK)
                    {
                        return checkFileResponse;
                    }
                }
            }
            DateTime salesCatalogueServiceRequestCompletedAt = DateTime.UtcNow;
            monitorHelper.MonitorRequest("Sales Catalogue Service Product Version Request", salesCatalogueServiceRequestStartedAt, salesCatalogueServiceRequestCompletedAt, request.CorrelationId, null, null, fileSize, null);

            var response = SetExchangeSetResponse(salesCatalogueResponse, true);

            if (isAioEnabled)//when toggle on then add additional aio cell details
            {
                response.ExchangeSetResponse.RequestedAioProductCount = aioCells.Count();
                response.ExchangeSetResponse.AioExchangeSetCellCount = aioCells.Count();
                response.ExchangeSetResponse.RequestedAioProductsAlreadyUpToDateCount = 0;
            }

            if (response.HttpStatusCode != HttpStatusCode.OK && response.HttpStatusCode != HttpStatusCode.NotModified)
            {
                return response;
            }

            if (salesCatalogueResponse.ResponseCode == HttpStatusCode.NotModified)
            {
                response.ExchangeSetResponse.RequestedProductCount = response.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount = request.ProductVersions.Count;
                response.ExchangeSetResponse.RequestedProductsNotInExchangeSet = new List<RequestedProductsNotInExchangeSet>();
                salesCatalogueResponse.ResponseBody = new SalesCatalogueProductResponse
                {
                    Products = new List<Products>(),
                    ProductCounts = new ProductCounts()
                };
                salesCatalogueResponse.ResponseBody.ProductCounts.ReturnedProductCount = 0;
                salesCatalogueResponse.ResponseBody.ProductCounts.RequestedProductsNotReturned = new List<RequestedProductsNotReturned>();
                salesCatalogueResponse.ResponseBody.ProductCounts.RequestedProductCount = salesCatalogueResponse.ResponseBody.ProductCounts.RequestedProductsAlreadyUpToDateCount = request.ProductVersions.Count;
            }

            var exchangeSetServiceResponse = await SetExchangeSetResponseLinks(response, request.CorrelationId);

            if (exchangeSetServiceResponse.HttpStatusCode != HttpStatusCode.Created)
                return exchangeSetServiceResponse;

            string expiryDate = exchangeSetServiceResponse.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

            if (!string.IsNullOrEmpty(exchangeSetServiceResponse.BatchId))
            {
                await SaveSalesCatalogueStorageDetails(salesCatalogueResponse.ResponseBody, exchangeSetServiceResponse.BatchId, request.CallbackUri, request.CorrelationId, expiryDate, salesCatalogueResponse.ScsRequestDateTime);
            }

            return response;
        }

        public Task<ValidationResult> ValidateProductDataByProductVersions(ProductDataProductVersionsRequest request)
        {
            return productVersionsValidator.Validate(request);
        }

        public async Task<ExchangeSetServiceResponse> CreateProductDataSinceDateTime(ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest, AzureAdB2C azureAdB2C)
        {
            DateTime salesCatalogueServiceRequestStartedAt = DateTime.UtcNow;
            var salesCatalogueResponse = await salesCatalogueService.GetProductsFromSpecificDateAsync(productDataSinceDateTimeRequest.SinceDateTime, productDataSinceDateTimeRequest.CorrelationId);
            long fileSize = 0;
            if (salesCatalogueResponse.ResponseCode == HttpStatusCode.OK)
            {
                fileSize = CommonHelper.GetFileSize(salesCatalogueResponse.ResponseBody);
                bool isAzureB2C = azureAdB2CHelper.IsAzureB2CUser(azureAdB2C, productDataSinceDateTimeRequest.CorrelationId);
                if (isAzureB2C)
                {
                    var checkFileResponse = CheckIfExchangeSetTooLarge(fileSize);
                    if (checkFileResponse.HttpStatusCode != HttpStatusCode.OK)
                    {
                        return checkFileResponse;
                    }
                }
            }
            DateTime salesCatalogueServiceRequestCompletedAt = DateTime.UtcNow;
            monitorHelper.MonitorRequest("Sales Catalogue Service Since DateTime Request", salesCatalogueServiceRequestStartedAt, salesCatalogueServiceRequestCompletedAt, productDataSinceDateTimeRequest.CorrelationId, null, null, fileSize, null);

            var response = SetExchangeSetResponse(salesCatalogueResponse, false);

            IEnumerable<string> aioCells = FilterAioCellsByProductData(salesCatalogueResponse.ResponseBody);
            bool isAioEnabled = aioConfiguration.Value.AioEnabled.HasValue && aioConfiguration.Value.AioEnabled.Value;

            if (isAioEnabled)//when toggle on then add additional aio cell details
            {
                response.ExchangeSetResponse.RequestedAioProductCount = aioCells.Count();
                response.ExchangeSetResponse.AioExchangeSetCellCount = aioCells.Count();
                response.ExchangeSetResponse.RequestedAioProductsAlreadyUpToDateCount = 0;
            }

            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                return response;
            }

            var exchangeSetServiceResponse = await SetExchangeSetResponseLinks(response, productDataSinceDateTimeRequest.CorrelationId);

            if (exchangeSetServiceResponse.HttpStatusCode != HttpStatusCode.Created)
                return exchangeSetServiceResponse;

            string expiryDate = exchangeSetServiceResponse.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

            if (!string.IsNullOrEmpty(exchangeSetServiceResponse.BatchId))
            {
                await SaveSalesCatalogueStorageDetails(salesCatalogueResponse.ResponseBody, exchangeSetServiceResponse.BatchId, productDataSinceDateTimeRequest.CallbackUri, productDataSinceDateTimeRequest.CorrelationId, expiryDate, salesCatalogueResponse.ScsRequestDateTime);
            }

            return response;
        }

        public Task<ValidationResult> ValidateProductDataSinceDateTime(ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest)
        {
            return productDataSinceDateTimeValidator.Validate(productDataSinceDateTimeRequest);
        }

        private ExchangeSetServiceResponse SetExchangeSetResponse(SalesCatalogueResponse salesCatalougeResponse, bool isNotModifiedToOk)
        {
            var response = new ExchangeSetServiceResponse
            {
                HttpStatusCode = salesCatalougeResponse.ResponseCode
            };
            if (salesCatalougeResponse.ResponseCode == HttpStatusCode.OK)
            {
                response.ExchangeSetResponse = MapExchangeSetResponse(salesCatalougeResponse);
                response.LastModified = ConvertLastModifiedToString(salesCatalougeResponse);
            }
            else if (salesCatalougeResponse.ResponseCode == HttpStatusCode.NotModified)
            {
                if (isNotModifiedToOk)
                {
                    response.HttpStatusCode = HttpStatusCode.OK;
                }
                response.ExchangeSetResponse = new ExchangeSetResponse();
                response.LastModified = ConvertLastModifiedToString(salesCatalougeResponse);
            }
            else
            {
                response.HttpStatusCode = HttpStatusCode.InternalServerError;
            }
            return response;
        }

        private Task<ExchangeSetServiceResponse> SetExchangeSetResponseLinks(ExchangeSetServiceResponse exchangeSetServiceResponse, string correlationId)
        {
            return logger.LogStartEndAndElapsedTimeAsync(EventIds.FSSCreateBatchRequestStart,
                EventIds.FSSCreateBatchRequestCompleted,
                "FSS create batch endpoint request for _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    var createBatchResponse =
                        await fileShareService.CreateBatch(userIdentifier.UserIdentity, correlationId);

                    bool isAioEnabled = aioConfiguration.Value.AioEnabled.HasValue && aioConfiguration.Value.AioEnabled.Value;

                    if (isAioEnabled)
                    {
                        logger.LogInformation(EventIds.AIOToggleIsOn.ToEventId(), "ESS API : AIO toggle is ON for BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}", createBatchResponse.ResponseBody.BatchId, correlationId);
                    }
                    else
                    {
                        logger.LogInformation(EventIds.AIOToggleIsOff.ToEventId(), "ESS API : AIO toggle is OFF for BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}", createBatchResponse.ResponseBody.BatchId, correlationId);
                    }

                    if (createBatchResponse.ResponseCode != HttpStatusCode.Created)
                    {
                        exchangeSetServiceResponse = new ExchangeSetServiceResponse
                        {
                            HttpStatusCode = HttpStatusCode.InternalServerError
                        };
                        return exchangeSetServiceResponse;
                    }

                    exchangeSetServiceResponse.ExchangeSetResponse.Links = new Links()
                    {
                        ExchangeSetBatchStatusUri = new LinkSetBatchStatusUri { Href = createBatchResponse.ResponseBody.BatchStatusUri },
                        ExchangeSetBatchDetailsUri = new LinkSetBatchDetailsUri { Href = createBatchResponse.ResponseBody.ExchangeSetBatchDetailsUri },
                        ExchangeSetFileUri = new LinkSetFileUri { Href = createBatchResponse.ResponseBody.ExchangeSetFileUri },
                        //when toggle on then add additional aio cell details
                        AioExchangeSetFileUri = isAioEnabled ? new LinkSetFileUri { Href = createBatchResponse.ResponseBody.AioExchangeSetFileUri } : null
                    };

                    exchangeSetServiceResponse.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime = Convert.ToDateTime(createBatchResponse.ResponseBody.BatchExpiryDateTime).ToUniversalTime();
                    exchangeSetServiceResponse.BatchId = createBatchResponse.ResponseBody.BatchId;
                    exchangeSetServiceResponse.ExchangeSetResponse.BatchId = exchangeSetServiceResponse.BatchId;
                    exchangeSetServiceResponse.HttpStatusCode = createBatchResponse.ResponseCode;

                    return exchangeSetServiceResponse;
                }, correlationId);
        }

        private ExchangeSetResponse MapExchangeSetResponse(SalesCatalogueResponse salesCatalougeResponse)
        {
            var model = mapper.Map<ExchangeSetResponse>(salesCatalougeResponse.ResponseBody?.ProductCounts);
            model.RequestedProductsNotInExchangeSet = mapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(salesCatalougeResponse.ResponseBody?.ProductCounts?.RequestedProductsNotReturned);
            return model;
        }

        private string ConvertLastModifiedToString(SalesCatalogueResponse salesCatalougeResponse)
        {
            return (salesCatalougeResponse.LastModified.HasValue) ? salesCatalougeResponse.LastModified.Value.ToString(RFC1123Format) : null;
        }

        private Task<bool> SaveSalesCatalogueStorageDetails(SalesCatalogueProductResponse salesCatalogueResponse, string batchId, string callBackUri, string correlationId, string expiryDate, DateTime scsRequestDateTime)
        {
            return logger.LogStartEndAndElapsedTimeAsync(EventIds.SCSResponseStoreRequestStart,
                EventIds.SCSResponseStoreRequestCompleted,
                "SCS response store request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    bool result = await exchangeSetStorageProvider.SaveSalesCatalogueStorageDetails(salesCatalogueResponse, batchId, callBackUri, correlationId, expiryDate, scsRequestDateTime);

                    return result;
                }, batchId, correlationId);

        }

        private IEnumerable<string> FilterAioCellsByProductIdentifiers(ProductIdentifierRequest products)
        {
            IEnumerable<string> configAioCells = !string.IsNullOrEmpty(aioConfiguration.Value.AioCells) ? new(aioConfiguration.Value.AioCells.Split(',')) : new List<string>();
            IEnumerable<string> aioCells = products.ProductIdentifier.Intersect(configAioCells).ToList();
            bool isAioEnabled = aioConfiguration.Value.AioEnabled.HasValue && aioConfiguration.Value.AioEnabled.Value;

            if (!isAioEnabled)//when toggle off then remove aio cells from scs request payload
            {
                products.ProductIdentifier = products.ProductIdentifier.Except(configAioCells).ToArray();
            }

            return aioCells;
        }

        private IEnumerable<string> FilterAioCellsByProductVersions(ProductDataProductVersionsRequest products)
        {
            IEnumerable<string> configAioCells = !string.IsNullOrEmpty(aioConfiguration.Value.AioCells) ? new(aioConfiguration.Value.AioCells.Split(',')) : new List<string>();
            IEnumerable<string> aioCells = products.ProductVersions.Select(x => x.ProductName).Intersect(configAioCells).ToList();
            bool isAioEnabled = aioConfiguration.Value.AioEnabled.HasValue && aioConfiguration.Value.AioEnabled.Value;

            if (!isAioEnabled)//when toggle off then remove aio cells from scs request payload
            {
                products.ProductVersions = products.ProductVersions.Where(x => !configAioCells.Any(y => y.Equals(x.ProductName))).ToList();
            }

            return aioCells;
        }

        private IEnumerable<string> FilterAioCellsByProductData(SalesCatalogueProductResponse products)
        {
            IEnumerable<string> configAioCells = !string.IsNullOrEmpty(aioConfiguration.Value.AioCells) ? new(aioConfiguration.Value.AioCells.Split(',')) : new List<string>();
            IEnumerable<string> aioCells = products != null ? products.Products.Select(p => p.ProductName).Intersect(configAioCells) : new List<string>();

            return aioCells;
        }
    }
}