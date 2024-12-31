// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using UKHO.SalesCatalogueFileShareServicesMock.API.Wiremock.Stubs;
using UKHO.SalesCatalogueFileShareServicesMock.API.WireMock.Stubs;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.WireMock.StubSetup
{
    public class StubFactory
    {
        public IStub CreateSCSStub()
        {
            return new SalesCatalogueServiceStub();
        }
    }
}
