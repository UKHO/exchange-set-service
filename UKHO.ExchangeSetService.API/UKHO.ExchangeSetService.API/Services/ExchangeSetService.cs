// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Extensions;
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.V2.Request;

namespace UKHO.ExchangeSetService.API.Services
{
    public class ExchangeSetService : IExchangeSetService
    {
        private readonly IUpdatesSinceValidator _updatesSinceValidator;

        public ExchangeSetService(IUpdatesSinceValidator updatesSinceValidator)
        {
            _updatesSinceValidator = updatesSinceValidator;
        }

        public async Task<ServiceResponseResult<ExchangeSetResponse>> CreateUpdateSince(UpdatesSinceRequest updatesSinceRequest, string CorrelationId, CancellationToken cancellationToken)
        {
            var validationResult = await _updatesSinceValidator.Validate(updatesSinceRequest);

            if (!validationResult.IsValid && validationResult.HasBadRequestErrors(out var errors))
            {
                return ServiceResponseResult<ExchangeSetResponse>.BadRequest(new ErrorDescription { CorrelationId = CorrelationId, Errors = errors });
            }

            return ServiceResponseResult<ExchangeSetResponse>.Accepted(new ExchangeSetResponse()); // This is a placeholder, the actual implementation is not provided
        }
    }
}
