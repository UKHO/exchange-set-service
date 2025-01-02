// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using UKHO.ExchangeSetService.Common.Logging;

namespace UKHO.ExchangeSetService.Common.Helpers.V2
{
    public class UriHelper : IUriHelper
    {
        private readonly ILogger<UriHelper> _logger;

        public UriHelper(ILogger<UriHelper> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Uri CreateUri(string baseUrl, string endpointFormat, string correlationId, params object[] args)
        {
            try
            {
                var formattedEndpoint = string.Format(endpointFormat, args);
                return new Uri(new Uri(baseUrl), formattedEndpoint);
            }
            catch (FormatException ex)
            {
                _logger.LogError(EventIds.UriException.ToEventId(), "Exception occurred while creating Uri. Error: {Error} | StackTrace: {StackTrace} | _X-Correlation-ID: {CorrelationId}", ex.Message, ex.StackTrace, correlationId);
                throw;
            }
        }
    }
}
