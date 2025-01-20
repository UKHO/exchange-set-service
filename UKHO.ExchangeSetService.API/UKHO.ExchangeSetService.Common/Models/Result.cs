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
        public ErrorResponse ErrorResponse { get; }
        public bool IsSuccess => StatusCode == HttpStatusCode.OK;

        protected Result(T value, HttpStatusCode statusCode, ErrorDescription errorDescription = null, ErrorResponse errorResponse = null)
        {
            Value = value;
            StatusCode = statusCode;
            ErrorDescription = errorDescription;
            ErrorResponse = errorResponse;
        }
    }
}
