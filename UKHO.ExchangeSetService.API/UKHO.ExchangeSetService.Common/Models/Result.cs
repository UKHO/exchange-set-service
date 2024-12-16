// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;

namespace UKHO.ExchangeSetService.Common.Models
{
    public class Result<T>
    {
        public T Value { get; }
        public HttpStatusCode StatusCode { get; }
        public ErrorResponse ErrorResponse { get; }

        protected Result(T value, HttpStatusCode statusCode, ErrorResponse errorResponse = null)
        {
            Value = value;
            StatusCode = statusCode;
            ErrorResponse = errorResponse;
        }

        public bool IsSuccess => StatusCode == HttpStatusCode.OK;
    }
}
