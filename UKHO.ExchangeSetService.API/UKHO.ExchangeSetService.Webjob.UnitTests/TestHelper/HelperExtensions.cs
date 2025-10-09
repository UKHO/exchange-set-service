using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.ExchangeSetService.Common.Logging;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.TestHelper
{
    internal static class HelperExtensions
    {
        public static void VerifyLogEntry<T>(this ILogger<T> logger, EventIds eventIds, string messageTemplate, bool endEvent = false, LogLevel logLevel = LogLevel.Information, int times = 1)
        {
            var eventId = eventIds.ToEventId();
            var messageFormat = endEvent ? messageTemplate + " Elapsed {Elapsed}" : messageTemplate;
            A.CallTo(logger).Where(call
                => call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == logLevel
                && call.GetArgument<EventId>(1) == eventId
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == messageFormat
                ).MustHaveHappened(times, Times.Exactly);
        }
    }
}
