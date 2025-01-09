// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace UKHO.ExchangeSetService.Common.Helpers.V2
{
    public interface IUriFactory
    {
        Uri CreateUri(string baseUrl, string endpointFormat, string correlationId, params object[] args);
    }
}
