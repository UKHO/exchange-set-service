// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.Common.Configuration.V2
{
    [ExcludeFromCodeCoverage]
    public class SalesCatalogueConfiguration
    {
        public string BaseUrl { get; set; }
        public string Version { get; set; }
        public string ResourceId { get; set; }
    }
}
