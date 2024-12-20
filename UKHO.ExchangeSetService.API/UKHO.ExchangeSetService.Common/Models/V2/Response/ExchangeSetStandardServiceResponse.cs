// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace UKHO.ExchangeSetService.Common.Models.V2.Response
{
    public class ExchangeSetStandardServiceResponse
    {
        public ExchangeSetStandardResponse ExchangeSetResponse { get; set; }

        public string LastModified { get; set; }

        public string BatchId { get; set; }
    }
}
