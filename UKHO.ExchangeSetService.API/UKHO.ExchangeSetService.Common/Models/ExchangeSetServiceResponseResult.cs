// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;

namespace UKHO.ExchangeSetService.Common.Models
{
    public class ExchangeSetServiceResponseResult<ExchangeSetResponse> : Result<ExchangeSetResponse>
    {
        public new ErrorResponse ErrorResponse { get; }

        private ExchangeSetServiceResponseResult(ExchangeSetResponse value, HttpStatusCode statusCode, ErrorResponse errorResponse = null)
            : base(value, statusCode, errorResponse)
        {
            ErrorResponse = errorResponse;
        }

        public static ExchangeSetServiceResponseResult<ExchangeSetResponse> Success(ExchangeSetResponse value) => new(value, HttpStatusCode.OK);

        public static ExchangeSetServiceResponseResult<ExchangeSetResponse> BadRequest(ErrorResponse errorResponse) => new(default, HttpStatusCode.BadRequest, errorResponse);
    }
}
