using System;
using System.Net;
using System.Net.Http;
using Polly;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public static class HttpRetryPolicies
    {
        public static IAsyncPolicy<HttpResponseMessage> NonSuccess(int retryCount, TimeSpan baseDelay)
        {
            return Policy
                .HandleResult<HttpResponseMessage>(r => r.StatusCode != HttpStatusCode.OK)
                .WaitAndRetryAsync(retryCount, retryAttempt => baseDelay * retryAttempt,
                    (result, _, _, _) => result.Result?.Dispose());
        }
    }
}
