// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using UKHO.SalesCatalogueFileShareServicesMock.API.Wiremock.Stubs;
using UKHO.SalesCatalogueFileShareServicesMock.API.WireMock.Configuration;
using UKHO.SalesCatalogueFileShareServicesMock.API.WireMock.Stubs;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.WireMock.StubSetup
{
    public class StubFactory
    {
        private readonly IOptions<SalesCatalogueServiceConfiguration> _salesCatalogueServiceConfiguration;

        public StubFactory(IOptions<SalesCatalogueServiceConfiguration> salesCatalogueServiceConfiguration)
        {
            _salesCatalogueServiceConfiguration = salesCatalogueServiceConfiguration;
        }
        public IStub CreateSCSStub()
        {
            return new ProductNamesStub(_salesCatalogueServiceConfiguration);
        }
    }
}
