﻿using AutoMapper;
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
using UKHO.ExchangeSetService.Common.Helpers.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.AzureADB2C;
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.API.Services
{
    public class ProductDataService(IProductIdentifierValidator productIdentifierValidator,
        IProductDataProductVersionsValidator productVersionsValidator,
        IScsProductIdentifierValidator scsProductIdentifierValidator,
        IProductDataSinceDateTimeValidator productDataSinceDateTimeValidator,
        ISalesCatalogueService salesCatalogueService,
        IMapper mapper,
        IFileShareService fileShareService,
        ILogger<ProductDataService> logger, IExchangeSetStorageProvider exchangeSetStorageProvider,
        IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageconfig, IMonitorHelper monitorHelper,
        UserIdentifier userIdentifier, IAzureAdB2CHelper azureAdB2CHelper, IOptions<AioConfiguration> aioConfiguration,
        IScsDataSinceDateTimeValidator scsDataSinceDateTimeValidator) : IProductDataService
    {
        private const string RFC1123Format = "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'";
        private readonly AioConfiguration aioConfiguration = aioConfiguration.Value;
        private bool isEmptyEncExchangeSet = false;
        private bool isEmptyAioExchangeSet = false;

        public async Task<ExchangeSetServiceResponse> CreateProductDataByProductIdentifiers(ProductIdentifierRequest productIdentifierRequest, AzureAdB2C azureAdB2C)
        {
            DateTime salesCatalogueServiceRequestStartedAt = DateTime.UtcNow;

            IEnumerable<string> aioCells = FilterAioCellsByProductIdentifiers(productIdentifierRequest).ToList();

            var salesCatalogueResponse = await salesCatalogueService.PostProductIdentifiersAsync(productIdentifierRequest.ProductIdentifier.ToList(), productIdentifierRequest.CorrelationId);
            long fileSize = 0;
            if (salesCatalogueResponse.ResponseCode == HttpStatusCode.OK)
            {
                fileSize = salesCatalogueResponse.ResponseBody.Products?.Sum(p => (long)p.FileSize) ?? 0;

                //check if exchangeSetStandard is S57                
                var checkS57File = CheckIfS57ExchangeSetTooLarge(fileSize, productIdentifierRequest.ExchangeSetStandard);
                if (checkS57File.HttpStatusCode != HttpStatusCode.OK)
                {
                    return checkS57File;
                }
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

            if (response.HttpStatusCode != HttpStatusCode.OK && response.HttpStatusCode != HttpStatusCode.NotModified)
            {
                return response;
            }

            //Set Aio details on exchange set response
            SetExchangeSetAioDetails(response.ExchangeSetResponse, productIdentifierRequest.ProductIdentifier.ToList(), salesCatalogueResponse.ResponseBody.Products, aioCells, response.BatchId, productIdentifierRequest.CorrelationId);

            var exchangeSetServiceResponse = await SetExchangeSetResponseLinks(response, productIdentifierRequest.CorrelationId);

            if (exchangeSetServiceResponse.HttpStatusCode != HttpStatusCode.Created)
            {
                return exchangeSetServiceResponse;
            }

            string expiryDate = exchangeSetServiceResponse.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

            if (!string.IsNullOrEmpty(exchangeSetServiceResponse.BatchId))
            {
                CheckEmptyExchangeSet(exchangeSetServiceResponse);
                var successful = await SaveSalesCatalogueStorageDetails(salesCatalogueResponse.ResponseBody, exchangeSetServiceResponse.BatchId, productIdentifierRequest.CallbackUri, productIdentifierRequest.ExchangeSetStandard, productIdentifierRequest.CorrelationId, expiryDate, salesCatalogueResponse.ScsRequestDateTime, isEmptyEncExchangeSet, isEmptyAioExchangeSet, exchangeSetServiceResponse.ExchangeSetResponse);
                if (!successful)
                {
                    logger.LogInformation(EventIds.CreateProductDataError.ToEventId(), "CreateProductDataByProductIdentifiers failed for BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}", exchangeSetServiceResponse.BatchId, productIdentifierRequest.CorrelationId);
                }
            }
            return response;
        }

        public async Task<SalesCatalogueResponse> CreateProductDataByProductIdentifiers(ScsProductIdentifierRequest scsProductIdentifierRequest)
        {
            var salesCatalogueResponse = await salesCatalogueService.PostProductIdentifiersAsync(scsProductIdentifierRequest.ProductIdentifier.ToList(), scsProductIdentifierRequest.CorrelationId);
            return salesCatalogueResponse;
        }

        private ExchangeSetServiceResponse CheckIfExchangeSetTooLarge(long fileSize)
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

        private ExchangeSetServiceResponse CheckIfS57ExchangeSetTooLarge(long fileSize, string exchangeSetStandard)
        {
            var fileSizeInMB = CommonHelper.ConvertBytesToMegabytes(fileSize);
            if (exchangeSetStandard == ExchangeSetStandard.s57.ToString() && fileSizeInMB >= essFulfilmentStorageconfig.Value.S57ExchangeSetSizeInMB)
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
        public Task<ValidationResult> ValidateScsProductDataByProductIdentifiers(ScsProductIdentifierRequest scsProductIdentifierRequest)
        {
            return scsProductIdentifierValidator.Validate(scsProductIdentifierRequest);
        }
        public async Task<ExchangeSetServiceResponse> CreateProductDataByProductVersions(ProductDataProductVersionsRequest request, AzureAdB2C azureAdB2C)
        {
            DateTime salesCatalogueServiceRequestStartedAt = DateTime.UtcNow;

            IEnumerable<string> aioCells = FilterAioCellsByProductVersions(request).ToList();

            var salesCatalogueResponse = await salesCatalogueService.PostProductVersionsAsync(request.ProductVersions, request.CorrelationId);
            long fileSize = 0;
            if (salesCatalogueResponse.ResponseCode == HttpStatusCode.OK)
            {
                fileSize = salesCatalogueResponse.ResponseBody.Products?.Sum(p => (long)p.FileSize) ?? 0;

                //check if exchangeSetStandard is S57                
                var checkS57File = CheckIfS57ExchangeSetTooLarge(fileSize, request.ExchangeSetStandard);
                if (checkS57File.HttpStatusCode != HttpStatusCode.OK)
                {
                    return checkS57File;
                }
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

            if (response.HttpStatusCode != HttpStatusCode.OK && response.HttpStatusCode != HttpStatusCode.NotModified)
            {
                return response;
            }

            if (salesCatalogueResponse.ResponseCode == HttpStatusCode.NotModified)
            {
                response.ExchangeSetResponse.RequestedProductCount = response.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount = request.ProductVersions.Count;
                response.ExchangeSetResponse.RequestedProductCount -= aioCells.Count();
                response.ExchangeSetResponse.RequestedProductsNotInExchangeSet = new List<RequestedProductsNotInExchangeSet>();
                response.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount = response.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount - aioCells.Count();
                response.ExchangeSetResponse.RequestedAioProductsAlreadyUpToDateCount = aioCells.Count();
                response.ExchangeSetResponse.AioExchangeSetCellCount = 0;
                response.ExchangeSetResponse.RequestedAioProductCount = aioCells.Count();
                salesCatalogueResponse.ResponseBody = new SalesCatalogueProductResponse
                {
                    Products = new List<Products>(),
                    ProductCounts = new ProductCounts()
                };
                salesCatalogueResponse.ResponseBody.ProductCounts.ReturnedProductCount = 0;
                salesCatalogueResponse.ResponseBody.ProductCounts.RequestedProductsNotReturned = new List<RequestedProductsNotReturned>();
                salesCatalogueResponse.ResponseBody.ProductCounts.RequestedProductCount = salesCatalogueResponse.ResponseBody.ProductCounts.RequestedProductsAlreadyUpToDateCount = request.ProductVersions.Count;
            }
            else if (salesCatalogueResponse.ResponseCode == HttpStatusCode.OK) //Set Aio details on exchange set response
            {
                IEnumerable<string> lstRequestedProducts = request.ProductVersions.Select(x => x.ProductName);
                SetExchangeSetAioDetails(response.ExchangeSetResponse, lstRequestedProducts, salesCatalogueResponse.ResponseBody.Products, aioCells, response.BatchId, request.CorrelationId);
            }

            var exchangeSetServiceResponse = await SetExchangeSetResponseLinks(response, request.CorrelationId);

            if (exchangeSetServiceResponse.HttpStatusCode != HttpStatusCode.Created)
                return exchangeSetServiceResponse;

            string expiryDate = exchangeSetServiceResponse.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

            if (!string.IsNullOrEmpty(exchangeSetServiceResponse.BatchId))
            {
                CheckEmptyExchangeSet(exchangeSetServiceResponse);

                var successful = await SaveSalesCatalogueStorageDetails(salesCatalogueResponse.ResponseBody, exchangeSetServiceResponse.BatchId, request.CallbackUri, request.ExchangeSetStandard, request.CorrelationId, expiryDate, salesCatalogueResponse.ScsRequestDateTime, isEmptyEncExchangeSet, isEmptyAioExchangeSet, exchangeSetServiceResponse.ExchangeSetResponse);
                if (!successful)
                {
                    logger.LogInformation(EventIds.CreateProductDataError.ToEventId(), "CreateProductDataByProductVersions failed for BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}", exchangeSetServiceResponse.BatchId, request.CorrelationId);
                }
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
                fileSize = salesCatalogueResponse.ResponseBody.Products?.Sum(p => (long)p.FileSize) ?? 0;

                //check if exchangeSetStandard is S57                
                var checkS57File = CheckIfS57ExchangeSetTooLarge(fileSize, productDataSinceDateTimeRequest.ExchangeSetStandard);
                if (checkS57File.HttpStatusCode != HttpStatusCode.OK)
                {
                    return checkS57File;
                }
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

            IEnumerable<string> aioCells = FilterAioCellsByProductData(salesCatalogueResponse.ResponseBody).ToList();

            var response = SetExchangeSetResponse(salesCatalogueResponse, false);

            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                return response;
            }
            //Set Aio details on exchange set response
            SetExchangeSetAioDetailsSinceDateTime(response.ExchangeSetResponse, aioCells, response.BatchId, productDataSinceDateTimeRequest.CorrelationId);

            var exchangeSetServiceResponse = await SetExchangeSetResponseLinks(response, productDataSinceDateTimeRequest.CorrelationId);

            if (exchangeSetServiceResponse.HttpStatusCode != HttpStatusCode.Created)
                return exchangeSetServiceResponse;

            string expiryDate = exchangeSetServiceResponse.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

            if (!string.IsNullOrEmpty(exchangeSetServiceResponse.BatchId))
            {
                var successful = await SaveSalesCatalogueStorageDetails(salesCatalogueResponse.ResponseBody, exchangeSetServiceResponse.BatchId, productDataSinceDateTimeRequest.CallbackUri, productDataSinceDateTimeRequest.ExchangeSetStandard, productDataSinceDateTimeRequest.CorrelationId, expiryDate, salesCatalogueResponse.ScsRequestDateTime, isEmptyEncExchangeSet, isEmptyAioExchangeSet, exchangeSetServiceResponse.ExchangeSetResponse);
                if (!successful)
                {
                    logger.LogInformation(EventIds.CreateProductDataError.ToEventId(), "CreateProductDataSinceDateTime failed for BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}", exchangeSetServiceResponse.BatchId, productDataSinceDateTimeRequest.CorrelationId);
                }
            }

            return response;
        }

        public Task<ValidationResult> ValidateProductDataSinceDateTime(ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest)
        {
            return productDataSinceDateTimeValidator.Validate(productDataSinceDateTimeRequest);
        }
        public async Task<SalesCatalogueResponse> GetProductDataSinceDateTime(ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest)
        {
            var salesCatalogueResponse = await salesCatalogueService.GetProductsFromSpecificDateAsync(productDataSinceDateTimeRequest.SinceDateTime, productDataSinceDateTimeRequest.CorrelationId);
            return salesCatalogueResponse;
        }

        public Task<ValidationResult> ValidateScsDataSinceDateTime(ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest)
        {
            return scsDataSinceDateTimeValidator.Validate(productDataSinceDateTimeRequest);
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

                    if (createBatchResponse.ResponseCode != HttpStatusCode.Created)
                    {
                        exchangeSetServiceResponse = new ExchangeSetServiceResponse
                        {
                            HttpStatusCode = HttpStatusCode.InternalServerError
                        };
                        return exchangeSetServiceResponse;
                    }

                    bool hasExchangeSetFileUri = exchangeSetServiceResponse.ExchangeSetResponse
                                                     .ExchangeSetCellCount > 0
                                                 || exchangeSetServiceResponse.ExchangeSetResponse
                                                     .RequestedProductsNotInExchangeSet.Any()
                                                 || exchangeSetServiceResponse.ExchangeSetResponse
                                                     .RequestedProductsAlreadyUpToDateCount > 0;

                    bool hasAioExchangeSetFileUri = exchangeSetServiceResponse.ExchangeSetResponse
                                                       .AioExchangeSetCellCount > 0
                                                   || exchangeSetServiceResponse.ExchangeSetResponse
                                                        .RequestedAioProductsAlreadyUpToDateCount > 0;

                    exchangeSetServiceResponse.ExchangeSetResponse.Links = new Links()
                    {
                        ExchangeSetBatchStatusUri = new LinkSetBatchStatusUri { Href = createBatchResponse.ResponseBody.BatchStatusUri },
                        ExchangeSetBatchDetailsUri = new LinkSetBatchDetailsUri { Href = createBatchResponse.ResponseBody.ExchangeSetBatchDetailsUri },
                        ExchangeSetFileUri = hasExchangeSetFileUri ? new LinkSetFileUri { Href = createBatchResponse.ResponseBody.ExchangeSetFileUri } : null,
                        AioExchangeSetFileUri = hasAioExchangeSetFileUri ?
                             new LinkSetFileUri { Href = createBatchResponse.ResponseBody.AioExchangeSetFileUri } : null
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
            model.RequestedProductsNotInExchangeSet = mapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(salesCatalougeResponse.ResponseBody?.ProductCounts?.RequestedProductsNotReturned).ToList();
            return model;
        }

        private static string ConvertLastModifiedToString(SalesCatalogueResponse salesCatalougeResponse)
        {
            return (salesCatalougeResponse.LastModified.HasValue) ? salesCatalougeResponse.LastModified.Value.ToString(RFC1123Format) : null;
        }

        private Task<bool> SaveSalesCatalogueStorageDetails(SalesCatalogueProductResponse salesCatalogueResponse, string batchId, string callBackUri, string exchangeSetStandard, string correlationId, string expiryDate, DateTime scsRequestDateTime, bool isEmptyEncExchangeSet, bool isEmptyAioExchangeSet, ExchangeSetResponse exchangeSetResponse)
        {
            return logger.LogStartEndAndElapsedTimeAsync(EventIds.SCSResponseStoreRequestStart,
                EventIds.SCSResponseStoreRequestCompleted,
                "SCS response store request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    bool result = await exchangeSetStorageProvider.SaveSalesCatalogueStorageDetails(salesCatalogueResponse, batchId, callBackUri, exchangeSetStandard, correlationId, expiryDate, scsRequestDateTime, isEmptyEncExchangeSet, isEmptyAioExchangeSet, exchangeSetResponse);

                    return result;
                }, batchId, correlationId);
        }

        private IEnumerable<string> FilterAioCellsByProductIdentifiers(ProductIdentifierRequest products)
        {
            IEnumerable<string> configAioCells = GetAioCells();
            IEnumerable<string> aioCells = products.ProductIdentifier.Intersect(configAioCells).ToList();

            return aioCells;
        }

        private IEnumerable<string> FilterAioCellsByProductVersions(ProductDataProductVersionsRequest products)
        {
            IEnumerable<string> configAioCells = GetAioCells();
            IEnumerable<string> aioCells = products.ProductVersions.Select(x => x.ProductName).Intersect(configAioCells).ToList();

            return aioCells;
        }

        private IEnumerable<string> FilterAioCellsByProductData(SalesCatalogueProductResponse products)
        {
            IEnumerable<string> configAioCells = GetAioCells();
            IEnumerable<string> aioCells = products != null ? products.Products.Select(p => p.ProductName).Intersect(configAioCells) : new List<string>();

            return aioCells;
        }

        private IEnumerable<string> GetAioCells()
        {
            return !string.IsNullOrEmpty(aioConfiguration.AioCells) ? new(aioConfiguration.AioCells.Split(',').Select(s => s.Trim())) : new List<string>();
        }

        private void SetExchangeSetAioDetails(ExchangeSetResponse exchangeSetResponse, IEnumerable<string> requestedProducts, IEnumerable<Products> scsResponseProducts, IEnumerable<string> aioCells, string batchId, string correlationId)
        {
            //filter valid and invalid aio/enc cells for calculations
            IEnumerable<string> invalidAioCells = exchangeSetResponse.RequestedProductsNotInExchangeSet.Where(x => aioCells.Any(y => y.Equals(x.ProductName))).Select(x => x.ProductName);
            IEnumerable<string> invalidEncCells = exchangeSetResponse.RequestedProductsNotInExchangeSet.Where(x => !aioCells.Any(y => y.Equals(x.ProductName))).Select(x => x.ProductName);

            IEnumerable<string> validAioCells = scsResponseProducts.Where(x => aioCells.Any(y => y.Equals(x.ProductName))).Select(x => x.ProductName);
            IEnumerable<string> validEncCells = scsResponseProducts.Where(x => !aioCells.Any(y => y.Equals(x.ProductName))).Select(x => x.ProductName);

            IEnumerable<string> totalAioCells = invalidAioCells.Concat(validAioCells);
            IEnumerable<string> totalEncCells = invalidEncCells.Concat(validEncCells);

            exchangeSetResponse.RequestedProductCount -= aioCells.Count();
            exchangeSetResponse.ExchangeSetCellCount = validEncCells.Count();

            var requestedEncCells = requestedProducts.Where(x => !aioCells.Any(y => y.Equals(x))).Select(x => x);
            exchangeSetResponse.RequestedProductsAlreadyUpToDateCount = requestedEncCells.Where(x => !totalEncCells.Any(y => y.Equals(x))).Count();//when requested enc cells are not found in response valid and invalid enc cells then cells are already uptodate

            //additional aio details
            exchangeSetResponse.RequestedAioProductCount = aioCells.Count();
            exchangeSetResponse.AioExchangeSetCellCount = validAioCells.Count();
            exchangeSetResponse.RequestedAioProductsAlreadyUpToDateCount = aioCells.Where(x => !totalAioCells.Any(y => y.Equals(x))).Count();//when requested aio cells are not found in response valid and invalid aio cells then cells are already uptodate

            logger.LogInformation(EventIds.AIOToggleIsOn.ToEventId(), "Aio cell details for AioCells:{AioCells} | BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}", string.Join(",", aioCells), batchId, correlationId);
        }

        private void SetExchangeSetAioDetailsSinceDateTime(ExchangeSetResponse exchangeSetResponse, IEnumerable<string> aioCells, string batchId, string correlationId)
        {
            exchangeSetResponse.ExchangeSetCellCount -= aioCells.Count();
            exchangeSetResponse.RequestedAioProductCount = 0;
            exchangeSetResponse.AioExchangeSetCellCount = aioCells.Count();
            exchangeSetResponse.RequestedAioProductsAlreadyUpToDateCount = 0;

            logger.LogInformation(EventIds.AIOToggleIsOn.ToEventId(), "Aio cell details for AioCells:{AioCells} | BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}", string.Join(",", aioCells), batchId, correlationId);
        }

        private void CheckEmptyExchangeSet(ExchangeSetServiceResponse exchangeSetServiceResponse)
        {
            //ProductVersion - 304 if AIO cell requested with latest edition and Update number then create empty AIO exchange set PBI #77585.
            isEmptyAioExchangeSet = exchangeSetServiceResponse.ExchangeSetResponse.AioExchangeSetCellCount == 0 && exchangeSetServiceResponse.ExchangeSetResponse.RequestedAioProductsAlreadyUpToDateCount > 0;

            //create standard empty exchange set when invalid enc or aio cell requested PBI #93502.
            isEmptyEncExchangeSet = exchangeSetServiceResponse.ExchangeSetResponse.ExchangeSetCellCount == 0 && exchangeSetServiceResponse.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount > 0 // when enc cell already up to date
                                    || exchangeSetServiceResponse.ExchangeSetResponse.RequestedProductsNotInExchangeSet.Any();// when invalid enc or aio cell requested
        }
    }
}
