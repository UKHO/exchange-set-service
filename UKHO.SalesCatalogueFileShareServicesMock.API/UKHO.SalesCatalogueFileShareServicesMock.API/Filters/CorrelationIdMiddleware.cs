using Microsoft.AspNetCore.Builder;
using System;
using System.Linq;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Filters
{
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
                    context.Request.Headers.Add(XCorrelationIdHeaderKey, correlationId);
                }

                context.Response.Headers.Add(XCorrelationIdHeaderKey, correlationId);

                await func();
            });
        }
    }
}
