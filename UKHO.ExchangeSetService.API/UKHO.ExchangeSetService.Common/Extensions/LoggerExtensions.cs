using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UKHO.ExchangeSetService.Common.Logging;

namespace UKHO.ExchangeSetService.Common.Extensions
{
    public static class LoggerExtensions
    {
        public static R LogStartEndAndElapsedTime<T, R>(this ILogger<T> logger,
            EventIds startEventId,
            EventIds completedEventId,
            string messageFormat,
            Func<R> func,
            params object[] messageArguments)
        {
            logger.LogInformation(startEventId.ToEventId(), messageFormat, messageArguments);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                return func();
            }
            finally
            {
                stopwatch.Stop();
                logger.LogInformation(completedEventId.ToEventId(),
                    messageFormat + " Elapsed {Elapsed}",
                    messageArguments.Concat(new object[] { stopwatch.Elapsed }).ToArray());
            }
        }

        public static async Task<TResult> LogStartEndAndElapsedTimeAsync<T, TResult>(this ILogger<T> logger,
            EventIds startEventId,
            EventIds completedEventId,
            string messageFormat,
            Func<Task<TResult>> func,
            params object[] messageArguments)
        {
            logger.LogInformation(startEventId.ToEventId(), messageFormat, messageArguments);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var result = await func();

            stopwatch.Stop();
            logger.LogInformation(completedEventId.ToEventId(),
                messageFormat + " Elapsed {Elapsed}",
                messageArguments.Concat(new object[] { stopwatch.Elapsed }).ToArray());

            return result;
        }
    }
}