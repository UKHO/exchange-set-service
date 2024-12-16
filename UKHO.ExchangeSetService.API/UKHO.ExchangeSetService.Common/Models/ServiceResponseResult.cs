// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;

namespace UKHO.ExchangeSetService.Common.Models
{
    public class ServiceResponseResult<T> : Result<T>
    {
        public new ErrorResponse ErrorResponse { get; }

        private ServiceResponseResult(T value, HttpStatusCode statusCode, ErrorResponse errorResponse = null)
            : base(value, statusCode, errorResponse)
        {
            ErrorResponse = errorResponse;
        }

        public static ServiceResponseResult<T> Success(T value) => new(value, HttpStatusCode.OK);

        public static ServiceResponseResult<T> NoContent() => new(default, HttpStatusCode.NoContent);

        public static ServiceResponseResult<T> NotModified() => new(default, HttpStatusCode.NotModified);

        public static ServiceResponseResult<T> NotFound(ErrorResponse errorResponse) => new(default, HttpStatusCode.NotFound, errorResponse);

        public static ServiceResponseResult<T> BadRequest(ErrorResponse errorResponse) => new(default, HttpStatusCode.BadRequest, errorResponse);

        public static ServiceResponseResult<T> NotAcceptable(ErrorResponse errorResponse) => new(default, HttpStatusCode.NotAcceptable, errorResponse);
    }
}
