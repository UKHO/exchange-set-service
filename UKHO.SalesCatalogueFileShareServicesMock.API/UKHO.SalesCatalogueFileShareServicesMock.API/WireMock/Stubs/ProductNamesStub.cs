// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net;
using Microsoft.Extensions.Options;
using UKHO.SalesCatalogueFileShareServicesMock.API.Wiremock.Stubs;
using UKHO.SalesCatalogueFileShareServicesMock.API.WireMock.Configuration;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;


namespace UKHO.SalesCatalogueFileShareServicesMock.API.WireMock.Stubs
{
    public class ProductNamesStub : IStub
    {
        private const string ResponseFileDirectory = @"WireMock\StubData\ProductNames";
        public const string exchangeSetStandard = "/productNames";

        private readonly string _responseFileDirectoryPath = Path.Combine(Environment.CurrentDirectory, ResponseFileDirectory);
        private readonly IOptions<SalesCatalogueServiceConfiguration> _salesCatalogueServiceConfiguration;

        public ProductNamesStub(IOptions<SalesCatalogueServiceConfiguration> salesCatalogueServiceConfiguration)
        {
            _salesCatalogueServiceConfiguration = salesCatalogueServiceConfiguration ?? throw new ArgumentNullException(nameof(salesCatalogueServiceConfiguration));
        }

        public void ConfigureStub(WireMockServer server)
        {
            server //200
               .Given(Request.Create()
               .WithPath(new WildcardMatcher(_salesCatalogueServiceConfiguration.Value.Url + exchangeSetStandard, true))
               .UsingPost() //need to add request body here              
               .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch))
               .RespondWith(Response.Create()
               .WithStatusCode(HttpStatusCode.Accepted)
                .WithBodyFromFile(Path.Combine(_responseFileDirectoryPath, "response-200.json")));

       //     server.Given(Request.Create().WithPath(ScsUrl).UsingGet())
     //.RespondWith(Response.Create().WithStatusCode(200).WithBody("OK"));
        }
    }
}
