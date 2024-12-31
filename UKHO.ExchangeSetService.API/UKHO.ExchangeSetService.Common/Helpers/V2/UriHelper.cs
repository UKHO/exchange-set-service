// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace UKHO.ExchangeSetService.Common.Helpers.V2
{
    public class UriHelper : IUriHelper
    {
        public Uri CreateUri(string baseUrl, string endpointFormat, params object[] args)
        {
            var formattedEndpoint = string.Format(endpointFormat, args);
            return new Uri(new Uri(baseUrl), formattedEndpoint);
        }
    }
}
