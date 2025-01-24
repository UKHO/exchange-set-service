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
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Helpers.V2;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue.V2;
using UKHO.ExchangeSetService.Common.Models.V2.Request;
using UKHO.ExchangeSetService.Common.Models.V2.Response;
using UKHO.ExchangeSetService.Common.Storage.V2;
using IFileShareService = UKHO.ExchangeSetService.Common.Helpers.IFileShareService;
using Links = UKHO.ExchangeSetService.Common.Models.Response.Links;
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
        private readonly IExchangeSetServiceStorageProvider _exchangeSetServiceStorageProvider;
        private bool isEmptyExchangeSet = false;

        private const string RFC3339Format = "yyyy-MM-ddTHH:mm:ss.fffZ";
        private const string S100ExchangeSetFileName = "S100.zip";

        public ExchangeSetStandardService(ILogger<ExchangeSetStandardService> logger,
            IUpdatesSinceValidator updatesSinceValidator,
            IProductVersionsValidator productVersionsValidator,
            IProductNameValidator productNameValidator,
            ISalesCatalogueService salesCatalogueService,
            IFileShareService fileShareService,
            UserIdentifier userIdentifier,
            IExchangeSetServiceStorageProvider exchangeSetServiceStorageProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _updatesSinceValidator = updatesSinceValidator ?? throw new ArgumentNullException(nameof(updatesSinceValidator));
            _productVersionsValidator = productVersionsValidator ?? throw new ArgumentNullException(nameof(productVersionsValidator));
            _productNameValidator = productNameValidator ?? throw new ArgumentNullException(nameof(productNameValidator));
            _salesCatalogueService = salesCatalogueService ?? throw new ArgumentNullException(nameof(salesCatalogueService));
            _fileShareService = fileShareService ?? throw new ArgumentNullException(nameof(fileShareService));
            _userIdentifier = userIdentifier ?? throw new ArgumentNullException(nameof(userIdentifier));
            _exchangeSetServiceStorageProvider = exchangeSetServiceStorageProvider ?? throw new ArgumentNullException(nameof(exchangeSetServiceStorageProvider));
        }

        /// <summary>
        /// Processes the product names request and returns the exchange set standard service response.
        /// </summary>
        /// <param name="productNames">Array of product names to be processed.</param>
        /// <param name="apiVersion">The API version to be used.</param>
        /// <param name="productType">The standard of the Exchange Set.</param>
        /// <param name="callbackUri">Optional callback URI for notification once the Exchange Set is ready.</param>
        /// <param name="correlationId">Guid based id for tracking the request.</param>
        /// <param name="cancellationToken">If true then notifies the underlying connection is aborted thus request operations should be cancelled.</param>
        /// <returns>Service response result containing the exchange set standard service response.</returns>
        public async Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> ProcessProductNamesRequestAsync(string[] productNames, ApiVersion apiVersion, string productType, string callbackUri, string correlationId, CancellationToken cancellationToken)
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

            var salesCatalogueServiceResponse = await _salesCatalogueService.PostProductNamesAsync(apiVersion, productType, productNamesRequest.ProductNames, correlationId, cancellationToken);

            if (salesCatalogueServiceResponse.IsSuccess)
            {
                var fssBatchResponse = await CreateFssBatchAsync(_userIdentifier.UserIdentity, correlationId);

                if(fssBatchResponse.ResponseCode != HttpStatusCode.Created)
                {
                    return ServiceResponseResult<ExchangeSetStandardServiceResponse>.InternalServerError();
                }
                
                var essResponse = SetExchangeSetStandardResponse(productNamesRequest, salesCatalogueServiceResponse, fssBatchResponse);

                var expiryDate = essResponse.Value.ExchangeSetStandardResponse.ExchangeSetUrlExpiryDateTime.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

                CheckEmptyExchangeSet(essResponse.Value);

                var success = await SaveSalesCatalogueStorageDetails(salesCatalogueServiceResponse.Value.ResponseBody, fssBatchResponse.ResponseBody.BatchId, callbackUri, productType, correlationId, expiryDate, salesCatalogueServiceResponse.Value.ScsRequestDateTime, isEmptyExchangeSet, essResponse.Value.ExchangeSetStandardResponse, apiVersion);
                if (!success)
                {
                    _logger.LogInformation(EventIds.CreateProductNamesError.ToEventId(), "ProcessProductNamesRequestAsync failed for BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}", fssBatchResponse.ResponseBody.BatchId, correlationId);
                }

                return essResponse;
            }
            
            return SetExchangeSetStandardResponse(productNamesRequest, salesCatalogueServiceResponse);
        }

        /// <summary>
        /// Processes the product versions request and returns the exchange set standard service response.
        /// </summary>
        /// <param name="productVersionRequest">Enumerable of product version requests to be processed.</param>
        /// <param name="apiVersion">The API version to be used.</param>
        /// <param name="productType">The standard of the Exchange Set.</param>
        /// <param name="callbackUri">Optional callback URI for notification once the Exchange Set is ready.</param>
        /// <param name="correlationId">Guid based id for tracking the request.</param>
        /// <param name="cancellationToken">If true then notifies the underlying connection is aborted thus request operations should be cancelled.</param>
        /// <returns>Service response result containing the exchange set standard service response.</returns>
        public async Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> ProcessProductVersionsRequestAsync(IEnumerable<ProductVersionRequest> productVersionRequest, ApiVersion apiVersion, string productType, string callbackUri, string correlationId, CancellationToken cancellationToken)
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

            var salesCatalogueServiceResponse = await _salesCatalogueService.PostProductVersionsAsync(apiVersion, productType, productVersionsRequest.ProductVersions, correlationId, cancellationToken);

            if (salesCatalogueServiceResponse.Value?.ResponseCode == HttpStatusCode.NotModified)
            {
                salesCatalogueServiceResponse.Value.ResponseBody = new V2SalesCatalogueProductResponse
                {
                    Products = [],
                    ProductCounts = new ProductCounts()
                };
                salesCatalogueServiceResponse.Value.ResponseBody.ProductCounts.ReturnedProductCount = 0;
                salesCatalogueServiceResponse.Value.ResponseBody.ProductCounts.RequestedProductsNotReturned = [];
                salesCatalogueServiceResponse.Value.ResponseBody.ProductCounts.RequestedProductCount = salesCatalogueServiceResponse.Value.ResponseBody.ProductCounts.RequestedProductsAlreadyUpToDateCount = productVersionsRequest.ProductVersions.Count();
            }

            if (salesCatalogueServiceResponse.Value?.ResponseCode == HttpStatusCode.NotModified || salesCatalogueServiceResponse.IsSuccess)
            {
                var fssBatchResponse = await CreateFssBatchAsync(_userIdentifier.UserIdentity, correlationId);
                if (fssBatchResponse.ResponseCode != HttpStatusCode.Created)
                {
                    return ServiceResponseResult<ExchangeSetStandardServiceResponse>.InternalServerError();
                }

                var essResponse = SetExchangeSetStandardResponse(productVersionsRequest, salesCatalogueServiceResponse, fssBatchResponse);

                var expiryDate = essResponse.Value.ExchangeSetStandardResponse.ExchangeSetUrlExpiryDateTime.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

                CheckEmptyExchangeSet(essResponse.Value);

                var success = await SaveSalesCatalogueStorageDetails(salesCatalogueServiceResponse.Value.ResponseBody, fssBatchResponse.ResponseBody.BatchId, callbackUri, productType, correlationId, expiryDate, salesCatalogueServiceResponse.Value.ScsRequestDateTime, isEmptyExchangeSet, essResponse.Value.ExchangeSetStandardResponse, apiVersion);
                if (!success)
                {
                    _logger.LogInformation(EventIds.CreateProductVersionError.ToEventId(), "ProcessProductVersionsRequestAsync failed for BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}", fssBatchResponse.ResponseBody.BatchId, correlationId);
                }

                return essResponse;
            }

            return SetExchangeSetStandardResponse(productVersionsRequest, salesCatalogueServiceResponse);
        }

        /// <summary>
        /// Processes the updates since request and returns the exchange set standard service response.
        /// </summary>
        /// <param name="updatesSinceRequest">Request containing the sinceDateTime parameter.</param>
        /// <param name="apiVersion">The API version to be used.</param>
        /// <param name="productType">The standard of the Exchange Set.</param>
        /// <param name="productIdentifier">Optional product identifier for filtering the updates.</param>
        /// <param name="callbackUri">Optional callback URI for notification once the Exchange Set is ready.</param>
        /// <param name="correlationId">Guid based id for tracking the request.</param>
        /// <param name="cancellationToken">If true then notifies the underlying connection is aborted thus request operations should be cancelled.</param>
        /// <returns>Service response result containing the exchange set standard service response.</returns>
        public async Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> ProcessUpdatesSinceRequestAsync(UpdatesSinceRequest updatesSinceRequest, ApiVersion apiVersion, string productType, string productIdentifier, string callbackUri, string correlationId, CancellationToken cancellationToken)
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

            var salesCatalogueServiceResponse = await _salesCatalogueService.GetProductsFromUpdatesSinceAsync(apiVersion, productType, updatesSinceRequest, correlationId, cancellationToken);

            if (salesCatalogueServiceResponse.IsSuccess)
            {
                var fssBatchResponse = await CreateFssBatchAsync(_userIdentifier.UserIdentity, correlationId);
                if (fssBatchResponse.ResponseCode != HttpStatusCode.Created)
                {
                    return ServiceResponseResult<ExchangeSetStandardServiceResponse>.InternalServerError();
                }

                var essResponse = SetExchangeSetStandardResponse(updatesSinceRequest, salesCatalogueServiceResponse, fssBatchResponse);

                var expiryDate = essResponse.Value.ExchangeSetStandardResponse.ExchangeSetUrlExpiryDateTime.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

                CheckEmptyExchangeSet(essResponse.Value);

                var success = await SaveSalesCatalogueStorageDetails(salesCatalogueServiceResponse.Value.ResponseBody, fssBatchResponse.ResponseBody.BatchId, callbackUri, productType, correlationId, expiryDate, salesCatalogueServiceResponse.Value.ScsRequestDateTime, isEmptyExchangeSet, essResponse.Value.ExchangeSetStandardResponse, apiVersion, productIdentifier);
                if (!success)
                {
                    _logger.LogInformation(EventIds.CreateUpdateSinceError.ToEventId(), "ProcessUpdatesSinceRequestAsync failed for BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}", fssBatchResponse.ResponseBody.BatchId, correlationId);
                }

                return essResponse;
            }

            return SetExchangeSetStandardResponse(updatesSinceRequest, salesCatalogueServiceResponse);
        }

        /// <summary>
        /// Sanitizes the product names by trimming and removing empty or null values.
        /// </summary>
        /// <param name="productNames">Enumerable of product names to be sanitized.</param>
        /// <returns>Array of sanitized product names.</returns>
        private static string[] SanitizeProductNames(IEnumerable<string> productNames)
        {
            return productNames?.Where(name => !string.IsNullOrEmpty(name))
                                .Select(name => name.Trim())
                                .ToArray();
        }

        /// <summary>
        /// Validates the request and returns the validation result.
        /// </summary>
        /// <typeparam name="T">Type of the request to be validated.</typeparam>
        /// <param name="request">Request to be validated.</param>
        /// <param name="correlationId">Guid based id for tracking the request.</param>
        /// <returns>Validation result containing the validation errors if any.</returns>
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

        /// <summary>
        /// Returns a bad request error response.
        /// </summary>
        /// <param name="correlationId">Guid based id for tracking the request.</param>
        /// <returns>Service response result containing the bad request error description.</returns>
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

        /// <summary>
        /// Sets the exchange set standard response based on the sales catalogue result.
        /// </summary>
        /// <typeparam name="R">Type of the request.</typeparam>
        /// <typeparam name="T">Type of the sales catalogue result.</typeparam>
        /// <param name="request">Request to be processed.</param>
        /// <param name="salesCatalogueResult">Sales catalogue result containing the response.</param>
        /// <returns>Service response result containing the exchange set standard service response.</returns>
        private static ServiceResponseResult<ExchangeSetStandardServiceResponse> SetExchangeSetStandardResponse<R, T>(R request, ServiceResponseResult<T> salesCatalogueResult)
        {
            var productCounts = (salesCatalogueResult.Value as V2SalesCatalogueResponse)?.ResponseBody?.ProductCounts;
            var lastModified = (salesCatalogueResult.Value as V2SalesCatalogueResponse)?.LastModified?.ToString("R");

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

        /// <summary>
        /// Sets the Exchange Set Standard Response based on the sales catalog response and file share service batch response.
        /// </summary>
        /// <typeparam name="R">Type of the request.</typeparam>
        /// <typeparam name="T">Type of the sales catalog response value.</typeparam>
        /// <param name="request">The request object.</param>
        /// <param name="salesCatalogResponse">The sales catalog response.</param>
        /// <param name="fssBatchResponse">The file share service batch response.</param>
        /// <returns>Service response result containing the Exchange Set Standard Service Response.</returns>
        private static ServiceResponseResult<ExchangeSetStandardServiceResponse> SetExchangeSetStandardResponse<R, T>(
            R request, Result<T> salesCatalogResponse, CreateBatchResponse fssBatchResponse)
        {
            var productCounts = (salesCatalogResponse.Value as V2SalesCatalogueResponse)?.ResponseBody?.ProductCounts;
            var lastModified = (salesCatalogResponse.Value as V2SalesCatalogueResponse)?.LastModified?.ToString("R");

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

        /// <summary>
        /// Creates a batch in the File Share Service (FSS) asynchronously.
        /// </summary>
        /// <param name="userIdentity">The user identity.</param>
        /// <param name="correlationId">The correlation ID for tracking the request.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the CreateBatchResponse.</returns>
        private Task<CreateBatchResponse> CreateFssBatchAsync(string userIdentity, string correlationId)
        {
            return _logger.LogStartEndAndElapsedTimeAsync(EventIds.FSSCreateBatchRequestStart,
                EventIds.FSSCreateBatchRequestCompleted,
                "FSS create batch endpoint request for _X-Correlation-ID:{correlationId}",
                async () =>
                {
                    var createBatchResponse = await _fileShareService.CreateBatch(userIdentity, correlationId);
                    return createBatchResponse;
                }, correlationId);
        }

        /// <summary>
        /// Saves the sales catalogue storage details asynchronously.
        /// </summary>
        /// <param name="salesCatalogueResponse">The sales catalogue response.</param>
        /// <param name="batchId">The batch identifier.</param>
        /// <param name="callBackUri">The callback URI.</param>
        /// <param name="exchangeSetStandard">The exchange set standard.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="expiryDate">The expiry date.</param>
        /// <param name="scsRequestDateTime">The SCS request date and time.</param>
        /// <param name="isEmptyExchangeSet">if set to <c>true</c> [is empty ENC exchange set].</param>
        /// <param name="exchangeSetStandardResponse">The exchange set standard response.</param>
        /// <param name="apiVersion">The API version.</param>
        /// <param name="productIdentifier">The product identifier.</param>
        /// <returns>A task that represents the asynchronous save operation. The task result contains a boolean indicating success or failure.</returns>
        private Task<bool> SaveSalesCatalogueStorageDetails(V2SalesCatalogueProductResponse salesCatalogueResponse, string batchId, string callBackUri, string exchangeSetStandard, string correlationId, string expiryDate, DateTime scsRequestDateTime, bool isEmptyExchangeSet, ExchangeSetStandardResponse exchangeSetStandardResponse, ApiVersion apiVersion, string productIdentifier = "")
        {
            return _logger.LogStartEndAndElapsedTimeAsync(EventIds.StoreResponseRequestStart,
                    EventIds.StoreResponseRequestCompleted,
                    "Response store request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                    async () =>
                    {
                        bool result = await _exchangeSetServiceStorageProvider.SaveSalesCatalogueStorageDetails(salesCatalogueResponse, batchId, callBackUri, exchangeSetStandard, correlationId, expiryDate, scsRequestDateTime, isEmptyExchangeSet, exchangeSetStandardResponse, apiVersion, productIdentifier);

                        return result;
                    }, batchId, correlationId);
        }

        /// <summary>
        /// Checks if the exchange set is empty based on the product counts in the response.
        /// </summary>
        /// <param name="exchangeSetServiceResponse">The response containing the exchange set details.</param>
        private void CheckEmptyExchangeSet(ExchangeSetStandardServiceResponse exchangeSetServiceResponse)
        {
            isEmptyExchangeSet = exchangeSetServiceResponse.ExchangeSetStandardResponse.ExchangeSetProductCount == 0 && exchangeSetServiceResponse.ExchangeSetStandardResponse.RequestedProductsAlreadyUpToDateCount > 0
                || exchangeSetServiceResponse.ExchangeSetStandardResponse.RequestedProductsNotInExchangeSet.Any();
        }
    }
}
