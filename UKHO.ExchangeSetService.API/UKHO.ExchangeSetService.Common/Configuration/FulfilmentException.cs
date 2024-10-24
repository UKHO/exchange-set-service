using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.Common.Configuration
{
    [ExcludeFromCodeCoverage]
    public class FulfilmentException : Exception
    {
        private static readonly string message = "There has been a problem in creating your exchange set, so we are unable to fulfil your request at this time. " +
            "Please contact UKHO Customer Services quoting error code : {0} and correlation ID : {1}";

        public EventId EventId { get; set; }

        public FulfilmentException(EventId eventId) : base(message)
        {
            EventId = eventId;
        }
    }
}
