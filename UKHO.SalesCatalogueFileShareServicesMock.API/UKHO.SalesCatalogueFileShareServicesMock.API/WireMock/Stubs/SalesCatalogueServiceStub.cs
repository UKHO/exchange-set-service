// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.IO;
using UKHO.SalesCatalogueFileShareServicesMock.API.Wiremock.Stubs;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;


namespace UKHO.SalesCatalogueFileShareServicesMock.API.WireMock.Stubs
{
    public class SalesCatalogueServiceStub : IStub
    {
        private const string ResponseFileDirectory = @"StubData\SCS";
        public const string ScsUrl = "/v2/products/s100/productNames";
        private readonly string _responseFileDirectoryPath = Path.Combine(Environment.CurrentDirectory, ResponseFileDirectory);

        public void ConfigureStub(WireMockServer server)
        {
            //server //200
            //   .Given(Request.Create()
            //   .WithPath(new WildcardMatcher(ScsUrl, true))
            //   .UsingPost()
            //   .WithBody(string.Empty)
            //   .WithHeader("Authorization", "Bearer *", MatchBehaviour.AcceptOnMatch))
            //   .RespondWith(Response.Create()
            //   .WithStatusCode(HttpStatusCode.Accepted)
            //    .WithBodyFromFile(Path.Combine(_responseFileDirectoryPath, "response-200.json")));

            server.Given(Request.Create().WithPath(ScsUrl).UsingGet())
      .RespondWith(Response.Create().WithStatusCode(200).WithBody("OK"));


        }
    }
}
