﻿using FluentValidation.Results;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.Request;

namespace UKHO.ExchangeSetService.API.Services
{
    public interface IEssWebhookService
    {
        Task<ValidationResult> ValidateEventGridCacheDataRequest(EnterpriseEventCacheDataRequest enterpriseEventCacheDataRequest);
        Task InvalidateAndInsertCacheDataAsync(EnterpriseEventCacheDataRequest enterpriseEventCacheDataRequest, string correlationId);
    }
}