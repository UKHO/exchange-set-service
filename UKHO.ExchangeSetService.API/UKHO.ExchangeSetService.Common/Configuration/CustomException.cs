using System;
using System.Runtime.Serialization;

namespace UKHO.ExchangeSetService.Common.Configuration
{
    [Serializable]
    public class CustomException : Exception
    {
        private static readonly string message = "There has been a problem in creating your exchange set, so we are unable to fulfil your request at this time. " +
            "Please contact UKHO Customer Services quoting error code : {0} and correlation ID : {1}";

        public CustomException() : base(message)
        {
        }

        public CustomException(string message, Exception innerException) : base(message, innerException)
        {
        }        

        protected CustomException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
