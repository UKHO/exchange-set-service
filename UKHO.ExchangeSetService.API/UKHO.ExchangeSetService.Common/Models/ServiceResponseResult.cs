﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using UKHO.ExchangeSetService.Common.Models.Response;

namespace UKHO.ExchangeSetService.Common.Models
{
    public class ServiceResponseResult<T> : Result<T>
    {
        public new ErrorDescription ErrorDescription { get; }

        private ServiceResponseResult(T value,
            HttpStatusCode statusCode,
            ErrorDescription errorDescription = null)
            : base(value, statusCode, errorDescription)
        {
            ErrorDescription = errorDescription;
        }

        public static ServiceResponseResult<T> Success(T value) => new(value, HttpStatusCode.OK);

        public static ServiceResponseResult<T> Accepted(T value) => new(value, HttpStatusCode.Accepted);

        public static ServiceResponseResult<T> NoContent() => new(default, HttpStatusCode.NoContent);

        public static ServiceResponseResult<T> NotModified() => new(default, HttpStatusCode.NotModified);

        public static ServiceResponseResult<T> NotFound(ErrorDescription errorDescription) => new(default, HttpStatusCode.NotFound, errorDescription);

        public static ServiceResponseResult<T> BadRequest(ErrorDescription errorDescription) => new(default, HttpStatusCode.BadRequest, errorDescription);

        public static ServiceResponseResult<T> NotAcceptable(ErrorDescription errorDescription) => new(default, HttpStatusCode.NotAcceptable, errorDescription);
    }
}