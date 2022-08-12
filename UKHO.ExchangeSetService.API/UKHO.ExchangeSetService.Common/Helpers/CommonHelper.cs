using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public static class CommonHelper
    {
        public static bool IsPeriodicOutputService { get; set; }

        public static IEnumerable<List<T>> SplitList<T>(List<T> masterList, int nSize = 30)
        {
            for (int i = 0; i < masterList.Count; i += nSize)
            {
                yield return masterList.GetRange(i, Math.Min(nSize, masterList.Count - i));
            }
        }

        public static int GetCurrentWeekNumber(DateTime date)
        {
            CultureInfo cultureInfo = CultureInfo.InvariantCulture;
            return cultureInfo.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFullWeek, DayOfWeek.Thursday);
        }

        public static string GetBlockIds(int blockNum)
        {
            string blockId = $"Block_{blockNum:00000}";
            return blockId;
        }

        public static byte[] CalculateMD5(byte[] requestBytes)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(requestBytes);

            return hash;
        }

        public static byte[] CalculateMD5(Stream requestStream)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(requestStream);

            return hash;
        }

        public static double ConvertBytesToMegabytes(long bytes)
        {
            double byteSize = 1024f;
            return (bytes / byteSize) / byteSize;
        }

        public static long GetFileSize(SalesCatalogueProductResponse salesCatalogueResponse)
        {
            long fileSize = 0;
            if (salesCatalogueResponse != null && salesCatalogueResponse.ProductCounts.ReturnedProductCount > 0)
            {
                foreach (var item in salesCatalogueResponse.Products)
                {
                    fileSize += item.FileSize.Value;
                }
            }
            return fileSize;
        }

        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger, string requestType, EventIds eventId, int retryCount, double sleepDuration)
        {
            return Policy
                .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.ServiceUnavailable)
                .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
                .OrResult(r => r.StatusCode == HttpStatusCode.InternalServerError && requestType == "File Share")
                .WaitAndRetryAsync(retryCount, (retryAttempt) =>
                {
                    return TimeSpan.FromSeconds(Math.Pow(sleepDuration, (retryAttempt - 1)));
                }, async (response, timespan, retryAttempt, context) =>
                {
                    var retryAfterHeader = response.Result.Headers.FirstOrDefault(h => h.Key.ToLowerInvariant() == "retry-after");
                    var correlationId = response.Result.RequestMessage.Headers.FirstOrDefault(h => h.Key.ToLowerInvariant() == "x-correlation-id");
                    int retryAfter = 0;
                    if (response.Result.StatusCode == HttpStatusCode.TooManyRequests && retryAfterHeader.Value != null && retryAfterHeader.Value.Any())
                    {
                        retryAfter = int.Parse(retryAfterHeader.Value.First());
                        await Task.Delay(TimeSpan.FromMilliseconds(retryAfter));
                    }
                    logger
                    .LogInformation(eventId.ToEventId(), "Re-trying {requestType} service request with uri {RequestUri} and delay {delay}ms and retry attempt {retry} with _X-Correlation-ID:{correlationId} as previous request was responded with {StatusCode}.",
                    requestType, response.Result.RequestMessage.RequestUri, timespan.Add(TimeSpan.FromMilliseconds(retryAfter)).TotalMilliseconds, retryAttempt, correlationId.Value, response.Result.StatusCode);
                });
        }
        public static bool IsNumeric(object Expression)
        {
            bool isNum = Double.TryParse(Convert.ToString(Expression), System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out _);
            return isNum;
        }
    }
}