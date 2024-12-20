// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UKHO.ExchangeSetService.API.Extensions;
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.V2.Request;
using UKHO.ExchangeSetService.Common.Models.V2.Response;

namespace UKHO.ExchangeSetService.API.Services
{
    public class ExchangeSetStandardService : IExchangeSetStandardService
    {
        private readonly ILogger<ExchangeSetStandardService> _logger;
        private readonly IUpdatesSinceValidator _updatesSinceValidator;

        public ExchangeSetStandardService(ILogger<ExchangeSetStandardService> logger, IUpdatesSinceValidator updatesSinceValidator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _updatesSinceValidator = updatesSinceValidator ?? throw new ArgumentNullException(nameof(updatesSinceValidator));
        }

        public async Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> CreateUpdatesSince(UpdatesSinceRequest updatesSinceRequest, string CorrelationId, CancellationToken cancellationToken)
        {
            _logger.LogInformation(EventIds.CreateUpdateSinceRequestStarted.ToEventId(), "Request to create update since started | _X-Correlation-ID : {CorrelationId}", CorrelationId);

            var validationResult = await _updatesSinceValidator.Validate(updatesSinceRequest);

            if (!validationResult.IsValid && validationResult.HasBadRequestErrors(out var errors))
            {
                _logger.LogError(EventIds.CreateUpdateSinceRequestException.ToEventId(), "Request to create update since exception occurred | _X-Correlation-ID : {CorrelationId}", CorrelationId);

                return ServiceResponseResult<ExchangeSetStandardServiceResponse>.BadRequest(new ErrorDescription { CorrelationId = CorrelationId, Errors = errors });
            }

            // This is a placeholder, the actual implementation is not provided
            var exchangeSetServiceResponse = new ExchangeSetStandardServiceResponse
            {
                BatchId = Guid.NewGuid().ToString(),
                LastModified = DateTime.UtcNow.ToString("R"),
                ExchangeSetResponse = new ExchangeSetStandardResponse()
                {
                    BatchId = Guid.NewGuid().ToString()
                }
            };

            _logger.LogInformation(EventIds.CreateUpdateSinceRequestCompleted.ToEventId(), "Request to create update since completed | _X-Correlation-ID : {CorrelationId}", CorrelationId);

            return ServiceResponseResult<ExchangeSetStandardServiceResponse>.Accepted(exchangeSetServiceResponse);
        }
    }
}
