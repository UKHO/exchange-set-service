using System.Diagnostics.CodeAnalysis;
using System.Net;
using UKHO.ExchangeSetService.Common.Models.Response;

namespace UKHO.ExchangeSetService.Common.Models
{
    [ExcludeFromCodeCoverage]
    public class Result<T>
    {
        public T Value { get; }
        public HttpStatusCode StatusCode { get; }
        public ErrorDescription ErrorDescription { get; }
        public NotFoundError NotFoundError { get; }

        protected Result(T value, HttpStatusCode statusCode, ErrorDescription errorDescription = null, NotFoundError notFoundError = null)
        {
            Value = value;
            StatusCode = statusCode;
            ErrorDescription = errorDescription;
            NotFoundError = notFoundError;
        }
    }
}
