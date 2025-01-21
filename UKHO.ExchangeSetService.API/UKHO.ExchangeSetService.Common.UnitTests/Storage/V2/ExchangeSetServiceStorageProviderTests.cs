// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers.V2;
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue.V2;
using UKHO.ExchangeSetService.Common.Models.V2.Response;
using UKHO.ExchangeSetService.Common.Storage.V2;


namespace UKHO.ExchangeSetService.Common.UnitTests.Storage.V2
{
    [TestFixture]
    public class ExchangeSetServiceStorageProviderTests
    {
        private IOptions<EssFulfilmentStorageConfiguration> _fakeStorageConfig;
        private IAzureStorageBlobService _fakeAzureStorageBlobService;
        private ExchangeSetServiceStorageProvider _exchangeSetServiceStorageProvider;

        [SetUp]
        public void Setup()
        {
            _fakeStorageConfig = A.Fake<IOptions<EssFulfilmentStorageConfiguration>>();
            _fakeAzureStorageBlobService = A.Fake<IAzureStorageBlobService>();
            _exchangeSetServiceStorageProvider = new ExchangeSetServiceStorageProvider(_fakeStorageConfig, _fakeAzureStorageBlobService);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullStorageConfig = () => new ExchangeSetServiceStorageProvider(null, _fakeAzureStorageBlobService);
            nullStorageConfig.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("storageConfig");

            Action nullAzureStorageBlobService = () => new ExchangeSetServiceStorageProvider(_fakeStorageConfig, null);
            nullAzureStorageBlobService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("azureStorageBlobService");
        }

        [Test]
        public async Task WhenCalledWithValidParameters_ThenShouldReturnTrue()
        {
            var salesCatalogueResponse = new V2SalesCatalogueProductResponse();
            var batchId = "batchId";
            var callBackUri = "http://callback.uri";
            var exchangeSetStandard = "standard";
            var correlationId = "correlationId";
            var expiryDate = "2023-12-31";
            var scsRequestDateTime = DateTime.UtcNow;
            var isEmptyExchangeSet = false;
            var exchangeSetResponse = new ExchangeSetStandardResponse();
            var apiVersion = ApiVersion.V2;
            var productIdentifier = "productIdentifier";

            A.CallTo(() => _fakeAzureStorageBlobService.StoreSalesCatalogueServiceResponseAsync(
                A<string>.Ignored, A<string>.Ignored, A<V2SalesCatalogueProductResponse>.Ignored, A<string>.Ignored,
                A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored, A<string>.Ignored,
                A<DateTime>.Ignored, A<bool>.Ignored, A<ExchangeSetStandardResponse>.Ignored, A<ApiVersion>.Ignored,
                A<string>.Ignored)).Returns(Task.FromResult(true));

            var result = await _exchangeSetServiceStorageProvider.SaveSalesCatalogueStorageDetails(
                salesCatalogueResponse, batchId, callBackUri, exchangeSetStandard, correlationId, expiryDate,
                scsRequestDateTime, isEmptyExchangeSet, exchangeSetResponse, apiVersion, productIdentifier);

            result.Should().BeTrue();
            A.CallTo(() => _fakeAzureStorageBlobService.StoreSalesCatalogueServiceResponseAsync(
                _fakeStorageConfig.Value.ExchangeSetStorageContainerName, batchId, salesCatalogueResponse, callBackUri,
                exchangeSetStandard, correlationId, A<CancellationToken>.Ignored, expiryDate, scsRequestDateTime,
                isEmptyExchangeSet, exchangeSetResponse, apiVersion, productIdentifier)).MustHaveHappenedOnceExactly();
        }
    }
}
