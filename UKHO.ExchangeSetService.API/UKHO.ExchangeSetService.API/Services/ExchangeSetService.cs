// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Extensions;
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.Response;

namespace UKHO.ExchangeSetService.API.Services
{
    public class ExchangeSetService : IExchangeSetService
    {
        private readonly IProductDataProductVersionsValidator _productVersionsValidator;

        public ExchangeSetService(IProductDataProductVersionsValidator productVersionsValidator)
        {
            _productVersionsValidator = productVersionsValidator ?? throw new ArgumentNullException(nameof(productVersionsValidator));
        }
        public async Task<ServiceResponseResult<ExchangeSetResponse>> CreateExchangeSetByProductVersions(ProductDataProductVersionsRequest productVersionsRequest, CancellationToken cancellationToken)
        {
            var validationResult = await _productVersionsValidator.Validate(productVersionsRequest);

            if (!validationResult.IsValid && validationResult.HasBadRequestErrors(out var errors))
            {
                return ServiceResponseResult<ExchangeSetResponse>.BadRequest(new ErrorDescription { CorrelationId = productVersionsRequest.CorrelationId, Errors = errors });
            }

            return ServiceResponseResult<ExchangeSetResponse>.Accepted(new ExchangeSetResponse()); // This is a placeholder, the actual implementation is not provided
        }        
    }
}
