using System;
using System.Runtime.Serialization;

namespace UKHO.ExchangeSetService.Common.Configuration
{
    [Serializable]
    public class CustomException : Exception
    {
        private static readonly string message = "There has been a problem in creating your exchange set, so we are unable to fulfil your request at this time. " +
            "Please contact UKHO Customer Services quoting error code XXX and correlation ID : correlationId";

        public string CorrelationId { get; set; }

        public string ErrorMessage { get; set; }

        public CustomException()
        {
        }

        public CustomException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public CustomException(string correlationId) : base(message)
        {
            CorrelationId = correlationId;
            ErrorMessage = message.Replace("correlationId", correlationId);
        }

        protected CustomException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
