using System;
using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ExchangeSetService.Common.Logging;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.TestHelper
{
    internal static class LoggerExtensions
    {
        public static void VerifyLogEntry<T>(this ILogger<T> logger, EventIds eventIds, string messageTemplate, bool endEvent = false, LogLevel logLevel = LogLevel.Information, int times = 1, bool checkIds = true)
        {
            var eventId = eventIds.ToEventId();
            var messageFormat = endEvent ? messageTemplate + " Elapsed {Elapsed}" : messageTemplate;
            A.CallTo(logger).Where(call
                => call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == logLevel
                && call.GetArgument<EventId>(1) == eventId
                && CheckParameters(call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!, messageFormat, checkIds)
                ).MustHaveHappened(times, Times.Exactly);
        }

        private static bool CheckParameters(IEnumerable<KeyValuePair<string, object>> keyValuePairs, string messageFormat, bool checkIds)
        {
            var dictionary = keyValuePairs.ToDictionary(c => c.Key, c => c.Value, StringComparer.OrdinalIgnoreCase);

            if (checkIds)
            {
                return dictionary["BatchId"].ToString() == FakeBatchValue.BatchId
                    && dictionary["CorrelationId"].ToString() == FakeBatchValue.CorrelationId
                    && dictionary["{OriginalFormat}"].ToString() == messageFormat;
            }
            else
            {
                return dictionary["{OriginalFormat}"].ToString() == messageFormat;
            }
        }
    }
}
