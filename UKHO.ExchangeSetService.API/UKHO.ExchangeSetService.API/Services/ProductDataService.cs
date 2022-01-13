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
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.AzureADB2C;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Storage;
using UKHO.ExchangeSetService.API.Configuration;
using UKHO.ExchangeSetService.Common.Models.AzureTableEntities;

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
        private readonly ILogger<FileShareService> logger;
        private readonly IExchangeSetStorageProvider exchangeSetStorageProvider;
        private readonly IOptions<AzureAdB2CConfiguration> azureAdB2CConfiguration;
        private readonly IOptions<AzureADConfiguration> azureAdConfiguration;
        private readonly IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageconfig;
        private readonly IMonitorHelper monitorHelper;
        private readonly UserIdentifier userIdentifier;
        private readonly IAzureTableStorageClient azureTableStorageClient;
        private readonly ISalesCatalogueStorageService azureStorageService;
        private readonly IAzureBlobStorageClient azureBlobStorageClient;
        private readonly IEventGridCacheDataRequestValidator eventGridCacheDataRequestValidator;
        private readonly IOptions<CacheConfiguration> cacheConfiguration;

        public ProductDataService(IProductIdentifierValidator productIdentifierValidator,
            IProductDataProductVersionsValidator productVersionsValidator,
            IProductDataSinceDateTimeValidator productDataSinceDateTimeValidator,
            ISalesCatalogueService salesCatalougeService,
            IMapper mapper,
            IFileShareService fileShareService,
            ILogger<FileShareService> logger, IExchangeSetStorageProvider exchangeSetStorageProvider,
            IOptions<AzureAdB2CConfiguration> azureAdB2CConfiguration, IOptions<AzureADConfiguration> azureAdConfiguration,
            IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageconfig, IMonitorHelper monitorHelper,
            UserIdentifier userIdentifier,
            IAzureTableStorageClient azureTableStorageClient,
            ISalesCatalogueStorageService azureStorageService,
            IAzureBlobStorageClient azureBlobStorageClient,
            IEventGridCacheDataRequestValidator eventGridCacheDataRequestValidator,
            IOptions<CacheConfiguration> cacheConfiguration)
        {
            this.productIdentifierValidator = productIdentifierValidator;
            this.productVersionsValidator = productVersionsValidator;
            this.productDataSinceDateTimeValidator = productDataSinceDateTimeValidator;
            this.salesCatalogueService = salesCatalougeService;
            this.mapper = mapper;
            this.fileShareService = fileShareService;
            this.logger = logger;
            this.exchangeSetStorageProvider = exchangeSetStorageProvider;
            this.azureAdB2CConfiguration = azureAdB2CConfiguration;
            this.azureAdConfiguration = azureAdConfiguration;
            this.essFulfilmentStorageconfig = essFulfilmentStorageconfig;
            this.monitorHelper = monitorHelper;
            this.userIdentifier = userIdentifier;
            this.azureTableStorageClient = azureTableStorageClient;
            this.azureStorageService = azureStorageService;
            this.azureBlobStorageClient = azureBlobStorageClient;
            this.eventGridCacheDataRequestValidator = eventGridCacheDataRequestValidator;
            this.cacheConfiguration = cacheConfiguration;
        }

        public async Task<ExchangeSetServiceResponse> CreateProductDataByProductIdentifiers(ProductIdentifierRequest productIdentifierRequest, AzureAdB2C azureAdB2C)
        {
            DateTime salesCatalogueServiceRequestStartedAt = DateTime.UtcNow;
            var salesCatalogueResponse = await salesCatalogueService.PostProductIdentifiersAsync(productIdentifierRequest.ProductIdentifier.ToList(), productIdentifierRequest.CorrelationId);
            long fileSize = 0;
            if (salesCatalogueResponse.ResponseCode == HttpStatusCode.OK)
            {
                fileSize = CommonHelper.GetFileSize(salesCatalogueResponse.ResponseBody);
                bool isAzureB2C = IsAzureB2CUser(azureAdB2C);
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
                await SaveSalesCatalogueStorageDetails(salesCatalogueResponse.ResponseBody, exchangeSetServiceResponse.BatchId, productIdentifierRequest.CallbackUri, productIdentifierRequest.CorrelationId, expiryDate);
            }

            return response;
        }

        public bool IsAzureB2CUser(AzureAdB2C azureAdB2C)
        {
            bool isAzureB2CUser = false;
            string b2CAuthority = $"{azureAdB2CConfiguration.Value.Instance}{azureAdB2CConfiguration.Value.TenantId}/v2.0/";// for B2C Token
            string adB2CAuthority = $"{azureAdConfiguration.Value.MicrosoftOnlineLoginUrl}{azureAdB2CConfiguration.Value.TenantId}/v2.0";// for AdB2C Token
            string audience = azureAdB2CConfiguration.Value.ClientId;
            if (azureAdB2C.IssToken == b2CAuthority && azureAdB2C.AudToken == audience)
            {
                isAzureB2CUser = true;
            }
            else if (azureAdB2C.IssToken == adB2CAuthority && azureAdB2C.AudToken == audience)
            {
                isAzureB2CUser = true;
            }
            return isAzureB2CUser;
        }
        public ExchangeSetServiceResponse CheckIfExchangeSetTooLarge(long fileSize)
        {
            var fileSizeInMB = CommonHelper.ConvertBytesToMegabytes(fileSize);
            if (fileSizeInMB >= essFulfilmentStorageconfig.Value.LargeExchangeSetSizeInMB)
            {
                ExchangeSetServiceResponse exchangeSetResponse = new ExchangeSetServiceResponse
                {
                    HttpStatusCode = HttpStatusCode.BadRequest,
                    IsExchangeSetTooLarge = true
                };
                return exchangeSetResponse;
            }
            else
            {
                ExchangeSetServiceResponse exchangeSetResponse = new ExchangeSetServiceResponse
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
            var salesCatalogueResponse = await salesCatalogueService.PostProductVersionsAsync(request.ProductVersions, request.CorrelationId);
            long fileSize = 0;
            if (salesCatalogueResponse.ResponseCode == HttpStatusCode.OK)
            {
                fileSize = CommonHelper.GetFileSize(salesCatalogueResponse.ResponseBody);
                bool isAzureB2C = IsAzureB2CUser(azureAdB2C);
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
                await SaveSalesCatalogueStorageDetails(salesCatalogueResponse.ResponseBody, exchangeSetServiceResponse.BatchId, request.CallbackUri, request.CorrelationId, expiryDate);
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
                bool isAzureB2C = IsAzureB2CUser(azureAdB2C);
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
                await SaveSalesCatalogueStorageDetails(salesCatalogueResponse.ResponseBody, exchangeSetServiceResponse.BatchId, productDataSinceDateTimeRequest.CallbackUri, productDataSinceDateTimeRequest.CorrelationId, expiryDate);
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

        private async Task<ExchangeSetServiceResponse> SetExchangeSetResponseLinks(ExchangeSetServiceResponse exchangeSetResponse, string correlationId)
        {
            logger.LogInformation(EventIds.FSSCreateBatchRequestStart.ToEventId(), "FSS create batch endpoint request started for _X-Correlation-ID:{CorrelationId}", correlationId);

            var createBatchResponse = await fileShareService.CreateBatch(userIdentifier.UserIdentity, correlationId);

            if (createBatchResponse.ResponseCode != HttpStatusCode.Created)
            {
                exchangeSetResponse = new ExchangeSetServiceResponse
                {
                    HttpStatusCode = HttpStatusCode.InternalServerError
                };
                return exchangeSetResponse;
            }

            exchangeSetResponse.ExchangeSetResponse.Links = new Links()
            {
                ExchangeSetBatchStatusUri = new LinkSetBatchStatusUri { Href = createBatchResponse.ResponseBody.BatchStatusUri },
                ExchangeSetBatchDetailsUri = new LinkSetBatchDetailsUri { Href = createBatchResponse.ResponseBody.ExchangeSetBatchDetailsUri },
                ExchangeSetFileUri = new LinkSetFileUri { Href = createBatchResponse.ResponseBody.ExchangeSetFileUri }
            };
            exchangeSetResponse.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime = Convert.ToDateTime(createBatchResponse.ResponseBody.BatchExpiryDateTime).ToUniversalTime();
            exchangeSetResponse.BatchId = createBatchResponse.ResponseBody.BatchId;
            exchangeSetResponse.HttpStatusCode = createBatchResponse.ResponseCode;

            logger.LogInformation(EventIds.FSSCreateBatchRequestCompleted.ToEventId(), "FSS create batch endpoint request completed with batch status uri {ExchangeSetBatchStatusUri.Href} for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", exchangeSetResponse.ExchangeSetResponse?.Links.ExchangeSetBatchStatusUri.Href, createBatchResponse.ResponseBody.BatchId, correlationId);

            return exchangeSetResponse;
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

        private async Task<bool> SaveSalesCatalogueStorageDetails(SalesCatalogueProductResponse salesCatalogueResponse, string batchId, string callBackUri, string correlationId, string expiryDate)
        {
            logger.LogInformation(EventIds.SCSResponseStoreRequestStart.ToEventId(), "SCS response store request started for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);

            bool result = await exchangeSetStorageProvider.SaveSalesCatalogueStorageDetails(salesCatalogueResponse, batchId, callBackUri, correlationId, expiryDate);

            logger.LogInformation(EventIds.SCSResponseStoreRequestCompleted.ToEventId(), "SCS response store request completed for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
            return result;
        }

        public Task<ValidationResult> ValidateEventGridCacheDataRequest(EventGridCacheDataRequest eventGridCacheDataRequest)
        {
            return eventGridCacheDataRequestValidator.Validate(eventGridCacheDataRequest);
        }

        public async Task<bool> DeleteSearchAndDownloadCacheData(EventGridCacheDataRequest eventGridCacheDataRequest, string correlationId)
        {
            var productCode = eventGridCacheDataRequest.Attributes.Where(a => a.Key == "ProductCode").Select(a => a.Value).FirstOrDefault();
            var cellName = eventGridCacheDataRequest.Attributes.Where(a => a.Key == "CellName").Select(a => a.Value).FirstOrDefault();
            var editionNumber = eventGridCacheDataRequest.Attributes.Where(a => a.Key == "EditionNumber").Select(a => a.Value).FirstOrDefault();
            var updateNumber = eventGridCacheDataRequest.Attributes.Where(a => a.Key == "UpdateNumber").Select(a => a.Value).FirstOrDefault();

            if (ValidateCacheAttributeData(eventGridCacheDataRequest.BusinessUnit, productCode, cellName, editionNumber, updateNumber))
            {
                var storageConnectionString = azureStorageService.GetStorageAccountConnectionString(cacheConfiguration.Value.CacheStorageAccountName, cacheConfiguration.Value.CacheStorageAccountKey);
                var cacheInfo = (FssSearchResponseCache)await azureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(cellName, editionNumber + "|" + updateNumber, cacheConfiguration.Value.FssSearchCacheTableName, storageConnectionString);

                if (cacheInfo != null && !string.IsNullOrEmpty(cacheInfo.Response))
                {
                    logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataEventStart.ToEventId(), "Search and Download cache data deletion from table and Blob started for ProductName:{cellName} and _X-Correlation-ID:{CorrelationId}", cellName, correlationId);

                    var cacheTableData = new CacheTableData
                    {
                        BatchId = cacheInfo.BatchId,
                        PartitionKey = cacheInfo.PartitionKey,
                        RowKey = cacheInfo.RowKey,
                        ETag = "*"
                    };
                    logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataTableStart.ToEventId(), "Search and Download cache data from table deletion started for ProductName:{cellName} and BatchId:{cacheInfo.BatchId} and _X-Correlation-ID:{CorrelationId}", cellName, cacheInfo.BatchId, correlationId);
                    await azureTableStorageClient.DeleteAsync(cacheTableData, cacheConfiguration.Value.FssSearchCacheTableName, storageConnectionString, essFulfilmentStorageconfig.Value.StorageContainerName);
                    logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataTableCompleted.ToEventId(), "Search and Download cache data from table deletion completed for table:{cacheConfiguration.Value.FssSearchCacheTableName} for BatchId:{cacheTableData.BatchId} and _X-Correlation-ID:{CorrelationId}", cacheConfiguration.Value.FssSearchCacheTableName, cacheTableData.BatchId, correlationId);

                    var response = await azureBlobStorageClient.DeleteCacheContainer(storageConnectionString, cacheTableData.BatchId);

                    logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataEventCompleted.ToEventId(), "Search and Download cache data from table and Blob completed for ProductName:{cellName} and BatchId:{cacheTableData.BatchId} and _X-Correlation-ID:{CorrelationId}", cellName, cacheTableData.BatchId, correlationId);
                    return response;
                }
                else
                {
                    logger.LogInformation(EventIds.DeleteSearchDownloadCacheNoDataFoundEvent.ToEventId(), "No product found in Search and Download Cache table:{cacheConfiguration.Value.FssSearchCacheTableName} and ProductName:{cellName} and _X-Correlation-ID:{CorrelationId}", cacheConfiguration.Value.FssSearchCacheTableName, cellName, correlationId);
                    return false;
                }
            }
            else
                logger.LogInformation(EventIds.DeleteInvalidSearchDownloadCacheDataFoundEvent.ToEventId(), "Inavlid data found in Search and Download Request for _X-Correlation-ID:{CorrelationId}", correlationId);
            return false;
        }

        private bool ValidateCacheAttributeData(string businessUnit, string productCode, string cellName, string editionNumber, string updateNumber)
        {
            return (businessUnit == cacheConfiguration.Value.CacheBusinessUnit && productCode == cacheConfiguration.Value.CacheProductCode && !string.IsNullOrWhiteSpace(cellName) && !string.IsNullOrWhiteSpace(editionNumber) && !string.IsNullOrWhiteSpace(updateNumber));
        }
    }
}