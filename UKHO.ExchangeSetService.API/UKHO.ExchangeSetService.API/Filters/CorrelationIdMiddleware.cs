using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace UKHO.ExchangeSetService.API.Filters
{
    [ExcludeFromCodeCoverage]
    public static class CorrelationIdMiddleware
    {
        public const string XCorrelationIdHeaderKey = "X-Correlation-ID";

        public static IApplicationBuilder UseCorrelationIdMiddleware(this IApplicationBuilder builder)
        {
            return builder.Use(async (context, func) =>
            {
                var correlationId = context.Request.Headers[XCorrelationIdHeaderKey].FirstOrDefault();

                if (string.IsNullOrEmpty(correlationId))
                {
                    correlationId = Guid.NewGuid().ToString();
                    context.Request.Headers.Append(XCorrelationIdHeaderKey,correlationId);
                }

                context.Response.Headers.Append(XCorrelationIdHeaderKey, correlationId);

                await func();
            });
        }
    }
}
