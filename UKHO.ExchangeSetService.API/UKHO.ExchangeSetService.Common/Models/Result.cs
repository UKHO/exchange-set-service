// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using UKHO.ExchangeSetService.Common.Models.Response;

namespace UKHO.ExchangeSetService.Common.Models
{
    public class Result<T>
    {
        public T Value { get; }
        public HttpStatusCode StatusCode { get; }
        public ErrorDescription ErrorDescription { get; }

        protected Result(T value, HttpStatusCode statusCode, ErrorDescription errorDescription = null)
        {
            Value = value;
            StatusCode = statusCode;
            ErrorDescription = errorDescription;
        }

        public bool IsSuccess => StatusCode == HttpStatusCode.OK;
    }
}
