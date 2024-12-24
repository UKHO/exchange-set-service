// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UKHO.ExchangeSetService.API.Extensions;
using UKHO.ExchangeSetService.API.Validation.V2;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.V2.Request;

namespace UKHO.ExchangeSetService.API.Services
{
    public class ExchangeSetStandardService : IExchangeSetStandardService
    {
        private readonly ILogger<ExchangeSetStandardService> _logger;
        private readonly IProductVersionsValidator _productVersionsValidator;

        public ExchangeSetStandardService(ILogger<ExchangeSetStandardService> logger, IProductVersionsValidator productVersionsValidator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _productVersionsValidator = productVersionsValidator ?? throw new ArgumentNullException(nameof(productVersionsValidator));
        }
        public async Task<ServiceResponseResult<ExchangeSetResponse>> CreateExchangeSetByProductVersions(ProductVersionsRequest productVersionsRequest, CancellationToken cancellationToken)
        {
            _logger.LogInformation(EventIds.CreateExchangeSetByProductVersionsStart.ToEventId(), "Exchange set creation for product versions started | X-Correlation-ID : {CorrelationId}", productVersionsRequest?.CorrelationId);

            if (productVersionsRequest?.ProductVersions == null || !productVersionsRequest.ProductVersions.Any() || productVersionsRequest.ProductVersions.Any(pv => pv == null))
            {
                var error = new List<Error>
                        {
                            new()
                            {
                                Source = "requestBody",
                                Description = "Either body is null or malformed."
                            }
                        };

                _logger.LogError(EventIds.CreateExchangeSetByProductVersionsException.ToEventId(), "Exchange set creation for product versions failed | X-Correlation-ID : {CorrelationId}", productVersionsRequest?.CorrelationId);

                return ServiceResponseResult<ExchangeSetResponse>.BadRequest(new ErrorDescription { CorrelationId = productVersionsRequest?.CorrelationId, Errors = error });
            }

            var validationResult = await _productVersionsValidator.Validate(productVersionsRequest);
            if (!validationResult.IsValid && validationResult.HasBadRequestErrors(out var errors))
            {
                _logger.LogError(EventIds.CreateExchangeSetByProductVersionsException.ToEventId(), "Exchange set creation for product versions failed | X-Correlation-ID : {CorrelationId}", productVersionsRequest.CorrelationId);

                return ServiceResponseResult<ExchangeSetResponse>.BadRequest(new ErrorDescription { CorrelationId = productVersionsRequest.CorrelationId, Errors = errors });
            }

            _logger.LogInformation(EventIds.CreateExchangeSetByProductVersionsCompleted.ToEventId(), "Exchange set creation for product versions completed | X-Correlation-ID : {CorrelationId}", productVersionsRequest.CorrelationId);

            return ServiceResponseResult<ExchangeSetResponse>.Accepted(new ExchangeSetResponse()); // This is a placeholder, the actual implementation is not provided
        }
    }
}
