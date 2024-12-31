// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using UKHO.SalesCatalogueFileShareServicesMock.API.Wiremock.Stubs;
using UKHO.SalesCatalogueFileShareServicesMock.API.WireMock.StubSetup;
using WireMock.Server;
using WireMock.Settings;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Wiremock.StubSetup
{
    public class StubManagerHostedService : IHostedService
    {
        private readonly WireMockServer _wireMockServer;
        private readonly StubFactory _stubFactory;

        public StubManagerHostedService(StubFactory stubFactory, IOptions<WireMockServerSettings> wireMockServerSettings)
        {
            _stubFactory = stubFactory;
            _wireMockServer = WireMockServer.Start(wireMockServerSettings.Value);
        }
        private void RegisterStubs()
        {
            RegisterStub(_stubFactory.CreateSCSStub());
        }

            public Task StartAsync(CancellationToken cancellationToken)
        {
            RegisterStubs();
            Console.WriteLine($"WireMock server is running at {_wireMockServer.Urls[0]}");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _wireMockServer.Stop();
            return Task.CompletedTask;
        }

        private void RegisterStub<T>(T stub) where T : IStub
        {
            stub.ConfigureStub(_wireMockServer);
        }
    }    
}
