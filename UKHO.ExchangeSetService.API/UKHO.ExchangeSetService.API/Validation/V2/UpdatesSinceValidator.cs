// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Configuration;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Models.V2.Request;

namespace UKHO.ExchangeSetService.API.Validation.V2
{
    public class UpdatesSinceValidator : AbstractValidator<UpdatesSinceRequest>, IUpdatesSinceValidator
    {
        private readonly IConfiguration _configuration;
        private DateTime _sinceDateTime;

        public UpdatesSinceValidator(IConfiguration configuration)
        {
            _configuration = configuration;

            RuleFor(x => x.SinceDateTime)
                .Must(x => x.IsValidIso8601Format(out _sinceDateTime))
                .WithMessage("Provided sinceDateTime is either invalid or invalid format, the valid format is 'ISO 8601 format' (e.g. '2024-12-20T11:51:00.000Z').")
                .WithErrorCode(HttpStatusCode.BadRequest.ToString())
                .DependentRules(() =>
                {
                    RuleFor(x => x.SinceDateTime)
                    .Must(x => DateTime.Compare(_sinceDateTime, DateTime.UtcNow) <= 0)
                    .WithMessage("Provided sinceDateTime cannot be a future date.")
                    .WithErrorCode(HttpStatusCode.BadRequest.ToString());
                    RuleFor(x => x.SinceDateTime)
                    .Must(x => DateTime.Compare(_sinceDateTime, DateTime.UtcNow.AddDays(-Convert.ToInt32(_configuration["MaximumNumerOfDaysValidForSinceDateTimeEndpoint"]))) > 0)
                    .WithMessage($"Provided sinceDateTime must be within last {configuration["MaximumNumerOfDaysValidForSinceDateTimeEndpoint"]} days.")
                    .WithErrorCode(HttpStatusCode.BadRequest.ToString());
                });

            RuleFor(x => x.CallbackUri)
                .Must(x => x.IsValidCallbackUri()).When(x => !string.IsNullOrEmpty(x.CallbackUri))
                .WithMessage("Invalid callbackUri format.")
                .WithErrorCode(HttpStatusCode.BadRequest.ToString());
        }

        Task<ValidationResult> IUpdatesSinceValidator.Validate(UpdatesSinceRequest updatesSinceRequest)
        {
            return ValidateAsync(updatesSinceRequest);
        }
    }
}
