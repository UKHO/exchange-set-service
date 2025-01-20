// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using UKHO.ExchangeSetService.API.Configuration;
using UKHO.ExchangeSetService.API.Extensions;
using UKHO.ExchangeSetService.API.Validation.V2;
using IFileShareService = UKHO.ExchangeSetService.Common.Helpers.IFileShareService;
using UKHO.ExchangeSetService.Common.Helpers.V2;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Models.V2.Request;
using UKHO.ExchangeSetService.Common.Models.V2.Response;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using Links = UKHO.ExchangeSetService.Common.Models.Response.Links;
using UKHO.ExchangeSetService.Common.Extensions;
using ProductVersionRequest = UKHO.ExchangeSetService.Common.Models.V2.Request.ProductVersionRequest;

namespace UKHO.ExchangeSetService.API.Services.V2
{
    public class ExchangeSetStandardService : IExchangeSetStandardService
    {
        private readonly ILogger<ExchangeSetStandardService> _logger;
        private readonly IUpdatesSinceValidator _updatesSinceValidator;
        private readonly IProductVersionsValidator _productVersionsValidator;
        private readonly IProductNameValidator _productNameValidator;
        private readonly ISalesCatalogueService _salesCatalogueService;
        private readonly IFileShareService _fileShareService;
        private readonly UserIdentifier _userIdentifier;

        private const string RFC3339Format = "yyyy-MM-ddTHH:mm:ss.fffZ";
        private const string S100ExchangeSetFileName = "S100.zip";

        public ExchangeSetStandardService(ILogger<ExchangeSetStandardService> logger,
            IUpdatesSinceValidator updatesSinceValidator,
            IProductVersionsValidator productVersionsValidator,
            IProductNameValidator productNameValidator,
            ISalesCatalogueService salesCatalogueService,
            IFileShareService fileShareService,
            UserIdentifier userIdentifier)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _updatesSinceValidator = updatesSinceValidator ?? throw new ArgumentNullException(nameof(updatesSinceValidator));
            _productVersionsValidator = productVersionsValidator ?? throw new ArgumentNullException(nameof(productVersionsValidator));
            _productNameValidator = productNameValidator ?? throw new ArgumentNullException(nameof(productNameValidator));
            _salesCatalogueService = salesCatalogueService ?? throw new ArgumentNullException(nameof(salesCatalogueService));
            _fileShareService = fileShareService ?? throw new ArgumentNullException(nameof(fileShareService));
            _userIdentifier = userIdentifier ?? throw new ArgumentNullException(nameof(userIdentifier));
        }

        public async Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> ProcessProductNamesRequestAsync(string[] productNames, ApiVersion apiVersion, string exchangeSetStandard, string callbackUri, string correlationId, CancellationToken cancellationToken)
        {
            productNames = SanitizeProductNames(productNames);

            if (productNames == null || productNames.Length == 0)
            {
                return BadRequestErrorResponse(correlationId);
            }

            var productNamesRequest = new ProductNameRequest
            {
                ProductNames = productNames,
                CallbackUri = callbackUri,
                CorrelationId = correlationId
            };

            var validationResult = await ValidateRequest(productNamesRequest, correlationId);
            if (validationResult != null)
            {
                return validationResult;
            }

            var salesCatalogServiceResponse = await _salesCatalogueService.PostProductNamesAsync(apiVersion, exchangeSetStandard, productNamesRequest.ProductNames, correlationId, cancellationToken);

            if (salesCatalogServiceResponse.IsSuccess)
            {
                var fssBatchResponse = await CreateFssBatchAsync(_userIdentifier.UserIdentity, correlationId);
                return fssBatchResponse.ResponseCode != HttpStatusCode.Created ?
                    ServiceResponseResult<ExchangeSetStandardServiceResponse>.InternalServerError() : SetExchangeSetStandardResponse(productNamesRequest, salesCatalogServiceResponse, fssBatchResponse);
            }

            return SetExchangeSetStandardResponse(productNamesRequest, salesCatalogServiceResponse);
        }

        public async Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> ProcessProductVersionsRequestAsync(IEnumerable<ProductVersionRequest> productVersionRequest, ApiVersion apiVersion, string exchangeSetStandard, string callbackUri, string correlationId, CancellationToken cancellationToken)
        {
            if (productVersionRequest == null || !productVersionRequest.Any() || productVersionRequest.Any(pv => pv == null))
            {
                return BadRequestErrorResponse(correlationId);
            }

            var productVersionsRequest = new ProductVersionsRequest
            {
                ProductVersions = productVersionRequest,
                CallbackUri = callbackUri
            };

            var validationResult = await ValidateRequest(productVersionsRequest, correlationId);
            if (validationResult != null)
            {
                return validationResult;
            }

            var salesCatalogServiceResponse = await _salesCatalogueService.PostProductVersionsAsync(apiVersion, exchangeSetStandard, productVersionsRequest.ProductVersions, correlationId, cancellationToken);

            //to be used while calling SaveSalesCatalogueStorageDetails
            if (salesCatalogServiceResponse.Value?.ResponseCode == HttpStatusCode.NotModified)
            {
                salesCatalogServiceResponse.Value.ResponseBody = new SalesCatalogueProductResponse
                {
                    Products = [],
                    ProductCounts = new ProductCounts()
                };
                salesCatalogServiceResponse.Value.ResponseBody.ProductCounts.ReturnedProductCount = 0;
                salesCatalogServiceResponse.Value.ResponseBody.ProductCounts.RequestedProductsNotReturned = [];
                salesCatalogServiceResponse.Value.ResponseBody.ProductCounts.RequestedProductCount = salesCatalogServiceResponse.Value.ResponseBody.ProductCounts.RequestedProductsAlreadyUpToDateCount = productVersionsRequest.ProductVersions.Count();
            }

            if (salesCatalogServiceResponse.Value?.ResponseCode == HttpStatusCode.NotModified || salesCatalogServiceResponse.IsSuccess)
            {
                var fssBatchResponse = await CreateFssBatchAsync(_userIdentifier.UserIdentity, correlationId);
                return fssBatchResponse.ResponseCode != HttpStatusCode.Created ?
                    ServiceResponseResult<ExchangeSetStandardServiceResponse>.InternalServerError() : SetExchangeSetStandardResponse(productVersionsRequest, salesCatalogServiceResponse, fssBatchResponse);
            }

            return SetExchangeSetStandardResponse(productVersionsRequest, salesCatalogServiceResponse);
        }

        public async Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> ProcessUpdatesSinceRequestAsync(UpdatesSinceRequest updatesSinceRequest, ApiVersion apiVersion, string exchangeSetStandard, string productIdentifier, string callbackUri, string correlationId, CancellationToken cancellationToken)
        {
            if (updatesSinceRequest?.SinceDateTime == null)
            {
                return BadRequestErrorResponse(correlationId);
            }

            updatesSinceRequest.ProductIdentifier = productIdentifier;
            updatesSinceRequest.CallbackUri = callbackUri;

            var validationResult = await ValidateRequest(updatesSinceRequest, correlationId);
            if (validationResult != null)
            {
                return validationResult;
            }

            var salesCatalogServiceResponse = await _salesCatalogueService.GetProductsFromUpdatesSinceAsync(apiVersion, exchangeSetStandard, updatesSinceRequest, correlationId, cancellationToken);

            if (salesCatalogServiceResponse.IsSuccess)
            {
                var fssBatchResponse = await CreateFssBatchAsync(_userIdentifier.UserIdentity, correlationId);
                return fssBatchResponse.ResponseCode != HttpStatusCode.Created ?
                    ServiceResponseResult<ExchangeSetStandardServiceResponse>.InternalServerError() : SetExchangeSetStandardResponse(updatesSinceRequest, salesCatalogServiceResponse, fssBatchResponse);
            }

            return SetExchangeSetStandardResponse(updatesSinceRequest, salesCatalogServiceResponse);
        }

        private static string[] SanitizeProductNames(IEnumerable<string> productNames)
        {
            return productNames?.Where(name => !string.IsNullOrEmpty(name))
                                .Select(name => name.Trim())
                                .ToArray();
        }

        private async Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> ValidateRequest<T>(T request, string correlationId)
        {
            var validationResult = request switch
            {
                UpdatesSinceRequest updatesSinceRequest => await _updatesSinceValidator.Validate(updatesSinceRequest),
                ProductNameRequest productNameRequest => await _productNameValidator.Validate(productNameRequest),
                ProductVersionsRequest productVersionsRequest => await _productVersionsValidator.Validate(productVersionsRequest),
                _ => throw new InvalidOperationException("Unsupported request type")
            };

            if (!validationResult.IsValid && validationResult.HasBadRequestErrors(out var errors))
            {
                _logger.LogError(EventIds.ValidationFailed.ToEventId(), "Validation failed for {RequestType} | errors : {errors} | _X-Correlation-ID : {correlationId}", typeof(T).Name, JsonConvert.SerializeObject(errors), correlationId);
                return ServiceResponseResult<ExchangeSetStandardServiceResponse>.BadRequest(new ErrorDescription { CorrelationId = correlationId, Errors = errors });
            }
            return null;
        }

        private ServiceResponseResult<ExchangeSetStandardServiceResponse> BadRequestErrorResponse(string correlationId)
        {
            _logger.LogError(EventIds.EmptyBodyError.ToEventId(), "Either body is null or malformed | _X-Correlation-ID : {correlationId}", correlationId);

            var errorDescription = new ErrorDescription
            {
                CorrelationId = correlationId,
                Errors = new List<Error>
                {
                    new()
                    {
                        Source = "requestBody",
                        Description = "Either body is null or malformed."
                    }
                }
            };
            return ServiceResponseResult<ExchangeSetStandardServiceResponse>.BadRequest(errorDescription);
        }

        private static ServiceResponseResult<ExchangeSetStandardServiceResponse> SetExchangeSetStandardResponse<R, T>(R request, ServiceResponseResult<T> salesCatalogueResult)
        {
            var lastModified = (salesCatalogueResult.Value as SalesCatalogueResponse)?.LastModified?.ToString("R");

            return salesCatalogueResult.StatusCode switch
            {
                HttpStatusCode.NotModified when request is UpdatesSinceRequest => ServiceResponseResult<ExchangeSetStandardServiceResponse>.NotModified(new ExchangeSetStandardServiceResponse
                {
                    ExchangeSetStandardResponse = new ExchangeSetStandardResponse(),
                    LastModified = lastModified
                }),
                HttpStatusCode.BadRequest => ServiceResponseResult<ExchangeSetStandardServiceResponse>.BadRequest(salesCatalogueResult.ErrorDescription),
                HttpStatusCode.NotFound => ServiceResponseResult<ExchangeSetStandardServiceResponse>.NotFound(salesCatalogueResult.ErrorResponse),
                _ => ServiceResponseResult<ExchangeSetStandardServiceResponse>.InternalServerError()
            };
        }

        //This method provide batch details and file uri for the exchange set standard response
        private static ServiceResponseResult<ExchangeSetStandardServiceResponse> SetExchangeSetStandardResponse<R, T>(
            R request, Result<T> salesCatalogResponse, CreateBatchResponse fssBatchResponse)
        {
            var productCounts = (salesCatalogResponse.Value as SalesCatalogueResponse)?.ResponseBody?.ProductCounts;
            var lastModified = (salesCatalogResponse.Value as SalesCatalogueResponse)?.LastModified?.ToString("R");

            var exchangeSetStandardServiceResponse = new ExchangeSetStandardServiceResponse
            {
                LastModified = lastModified,
                ExchangeSetStandardResponse = new ExchangeSetStandardResponse
                {
                    Links = new Links
                    {
                        ExchangeSetBatchStatusUri = new LinkSetBatchStatusUri { Href = fssBatchResponse.ResponseBody.BatchStatusUri },
                        ExchangeSetBatchDetailsUri = new LinkSetBatchDetailsUri { Href = fssBatchResponse.ResponseBody.ExchangeSetBatchDetailsUri },
                        ExchangeSetFileUri = new LinkSetFileUri { Href = $"{fssBatchResponse.ResponseBody.ExchangeSetBatchDetailsUri}/files/{S100ExchangeSetFileName}" }
                    },
                    ExchangeSetUrlExpiryDateTime = DateTime.ParseExact(fssBatchResponse.ResponseBody.BatchExpiryDateTime, RFC3339Format, CultureInfo.InvariantCulture).ToUniversalTime(),
                    BatchId = fssBatchResponse.ResponseBody.BatchId
                }
            };

            if (salesCatalogResponse.StatusCode == HttpStatusCode.NotModified && request is ProductVersionsRequest productVersionsRequest)
            {
                exchangeSetStandardServiceResponse.ExchangeSetStandardResponse.RequestedProductCount = productVersionsRequest.ProductVersions.Count();
                exchangeSetStandardServiceResponse.ExchangeSetStandardResponse.RequestedProductsAlreadyUpToDateCount = productVersionsRequest.ProductVersions.Count();
                exchangeSetStandardServiceResponse.ExchangeSetStandardResponse.RequestedProductsNotInExchangeSet = [];
                exchangeSetStandardServiceResponse.ExchangeSetStandardResponse.ExchangeSetProductCount = 0;

            }
            else if (salesCatalogResponse.StatusCode == HttpStatusCode.OK)
            {
                exchangeSetStandardServiceResponse.ExchangeSetStandardResponse.RequestedProductCount = productCounts.RequestedProductCount ?? 0;
                exchangeSetStandardServiceResponse.ExchangeSetStandardResponse.ExchangeSetProductCount = productCounts.ReturnedProductCount ?? 0;
                exchangeSetStandardServiceResponse.ExchangeSetStandardResponse.RequestedProductsAlreadyUpToDateCount = productCounts.RequestedProductsAlreadyUpToDateCount ?? 0;
                exchangeSetStandardServiceResponse.ExchangeSetStandardResponse.RequestedProductsNotInExchangeSet =
                    productCounts.RequestedProductsNotReturned
                        .Select(x =>
                            new RequestedProductsNotInExchangeSet { ProductName = x.ProductName, Reason = x.Reason })
                        .ToList();
            }

            return ServiceResponseResult<ExchangeSetStandardServiceResponse>.Accepted(exchangeSetStandardServiceResponse);
        }

        private Task<CreateBatchResponse> CreateFssBatchAsync(string userOid, string correlationId)
        {
            return _logger.LogStartEndAndElapsedTimeAsync(EventIds.FSSCreateBatchRequestStart,
                EventIds.FSSCreateBatchRequestCompleted,
                "FSS create batch endpoint request for _X-Correlation-ID:{correlationId}",
                async () =>
                {
                    var createBatchResponse = await _fileShareService.CreateBatch(userOid, correlationId);
                    return createBatchResponse;
                }, correlationId);
        }
    }
}
