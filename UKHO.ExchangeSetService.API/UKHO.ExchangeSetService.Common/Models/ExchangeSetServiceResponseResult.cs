// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using UKHO.ExchangeSetService.Common.Models.Response;

namespace UKHO.ExchangeSetService.Common.Models
{
    public class ExchangeSetServiceResponseResult<ExchangeSetResponse> : Result<ExchangeSetResponse>
    {
        public new ErrorDescription ErrorDescription { get; }

        private ExchangeSetServiceResponseResult(ExchangeSetResponse value, HttpStatusCode statusCode, ErrorDescription errorDescription = null)
            : base(value, statusCode, errorDescription)
        {
            ErrorDescription = errorDescription;
        }

        public static ExchangeSetServiceResponseResult<ExchangeSetResponse> Success(ExchangeSetResponse value) => new(value, HttpStatusCode.Accepted);

        public static ExchangeSetServiceResponseResult<ExchangeSetResponse> BadRequest(ErrorDescription errorDescription) => new(default, HttpStatusCode.BadRequest, errorDescription);
    }
}
