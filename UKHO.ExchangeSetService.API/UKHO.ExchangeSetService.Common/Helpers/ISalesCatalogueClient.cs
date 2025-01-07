// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface ISalesCatalogueClient
    {
        public Task<HttpResponseMessage> CallSalesCatalogueServiceApi(HttpMethod method, string requestBody, string authToken, string uri, string correlationId = "");
    }
}
