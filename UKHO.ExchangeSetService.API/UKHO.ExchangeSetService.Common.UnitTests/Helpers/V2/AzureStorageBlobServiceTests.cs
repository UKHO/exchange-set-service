// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Helpers.V2;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue.V2;
using UKHO.ExchangeSetService.Common.Models.V2.Response;

namespace UKHO.ExchangeSetService.Common.UnitTests.Helpers.V2
{
    [TestFixture]
    public class AzureStorageBlobServiceTests
    {
        private ILogger<AzureStorageBlobService> _fakeLogger;
        private IAzureBlobStorageClient _fakeAzureBlobStorageClient;
        private IAzureMessageQueueHelper _fakeAzureMessageQueueHelper;
        private IOptions<EssFulfilmentStorageConfiguration> _essFulfilmentStorageconfig;
        private AzureStorageBlobService _azureStorageBlobService;

        [SetUp]
        public void Setup()
        {
            _fakeLogger = A.Fake<ILogger<AzureStorageBlobService>>();
            _fakeAzureBlobStorageClient = A.Fake<IAzureBlobStorageClient>();
            _fakeAzureMessageQueueHelper = A.Fake<IAzureMessageQueueHelper>();
            _essFulfilmentStorageconfig = Options.Create(new EssFulfilmentStorageConfiguration
            {
                ExchangeSetStorageAccountName = "FakeAccountName",
                ExchangeSetStorageAccountKey = "FakeAccountKey"
            });

            _azureStorageBlobService = new AzureStorageBlobService(_fakeLogger, _fakeAzureBlobStorageClient, _fakeAzureMessageQueueHelper, _essFulfilmentStorageconfig);
        }

        [Test]
        public async Task WhenSuccessful_ThenShouldStoreResponseAndAddQueueMessage()
        {
            var containerName = "FakeContainer";
            var batchId = "FakeBatchId";
            var salesCatalogueResponse = new V2SalesCatalogueProductResponse();
            var callBackUri = "http://test.com";
            var exchangeSetStandard = "FakeStandard";
            var correlationId = "FakeCorrelationId";
            var cancellationToken = CancellationToken.None;
            var expiryDate = "2023-12-31";
            var scsRequestDateTime = DateTime.UtcNow;
            var isEmptyExchangeSet = false;
            var exchangeSetResponse = new ExchangeSetStandardResponse();
            var apiVersion = ApiVersion.V2;
            var productIdentifier = "FakeProduct";

            var fakeBlobClient = A.Fake<BlobClient>();
            A.CallTo(() => _fakeAzureBlobStorageClient.GetBlobClient(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(Task.FromResult(fakeBlobClient));
            A.CallTo(() => fakeBlobClient.UploadAsync(A<MemoryStream>.Ignored)).Returns(Task.FromResult(A.Dummy<Response<BlobContentInfo>>()));
            A.CallTo(() => fakeBlobClient.Uri).Returns(new Uri("https://fakeaccount.blob.core.windows.net/fakecontainer/fakeblob"));

            var result = await _azureStorageBlobService.StoreSalesCatalogueServiceResponseAsync(containerName, batchId, salesCatalogueResponse, callBackUri, exchangeSetStandard, correlationId, cancellationToken, expiryDate, scsRequestDateTime, isEmptyExchangeSet, exchangeSetResponse, apiVersion, productIdentifier);

            result.Should().BeTrue();
            A.CallTo(() => _fakeAzureMessageQueueHelper.AddMessage(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ResponseStoredToBlobStorage.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Response stored to blob storage with fileSizeInMB:{fileSizeInMB} for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId} ").MustHaveHappened();
        }

        [Test]
        public async Task WhenUploadFails_ThenShouldLogError()
        {
            var containerName = "FakeContainer";
            var batchId = "FakeBatchId";
            var salesCatalogueResponse = new V2SalesCatalogueProductResponse();
            var callBackUri = "http://test.com";
            var exchangeSetStandard = "FakeStandard";
            var correlationId = "FakeCorrelationId";
            var cancellationToken = CancellationToken.None;
            var expiryDate = "2023-12-31";
            var scsRequestDateTime = DateTime.UtcNow;
            var isEmptyExchangeSet = false;
            var exchangeSetResponse = new ExchangeSetStandardResponse();
            var apiVersion = ApiVersion.V2;
            var productIdentifier = "FakeProduct";

            var fakeBlobClient = A.Fake<BlobClient>();
            A.CallTo(() => _fakeAzureBlobStorageClient.GetBlobClient(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(Task.FromResult(fakeBlobClient));
            A.CallTo(() => fakeBlobClient.UploadAsync(A<MemoryStream>.Ignored)).ThrowsAsync(new RequestFailedException("Upload failed"));

            var result = await _azureStorageBlobService.StoreSalesCatalogueServiceResponseAsync(containerName, batchId, salesCatalogueResponse, callBackUri, exchangeSetStandard, correlationId, cancellationToken, expiryDate, scsRequestDateTime, isEmptyExchangeSet, exchangeSetResponse, apiVersion, productIdentifier);

            result.Should().BeFalse();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.StreamUploadFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Critical Error, stream upload failed: {message} stream source {sjo} ").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ResponseFailedStoredToBlobStorage.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Response not stored to blob storage for  fileSizeInMB:{fileSizeInMB} for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId} ").MustHaveHappened();
        }
    }
}
