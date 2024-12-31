// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using WireMock.Server;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Wiremock.Stubs
{
    public interface IStub
    {
        void ConfigureStub(WireMockServer server);
    }
}
