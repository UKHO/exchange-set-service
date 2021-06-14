using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage.Blob;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.FulfilmentService.Services;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Services
{
    [TestFixture]
    public class FulfilmentFileShareServiceTest
    {
        private IOptions<FileShareServiceConfiguration> fakefileShareServiceConfig;
        private IFileShareService fakefileShareService;
        private IAzureBlobStorageClient fakeazureBlobStorageClient;
        private IConfiguration fakeConfiguration;
        public FulfilmentFileShareService fulfilmentFileShareService;

        [SetUp]
        public void Setup()
        {
            fakefileShareService = A.Fake<IFileShareService>();
            fakeazureBlobStorageClient = A.Fake<IAzureBlobStorageClient>();
            fakeConfiguration = A.Fake<IConfiguration>();
            fakefileShareServiceConfig = Options.Create(new FileShareServiceConfiguration()
            { Limit=100,Start=0,ProductLimit=4,UpdateNumberLimit=10, EncRoot="ENC_ROOT", ExchangeSetFileFolder= "V01X01" });

            fulfilmentFileShareService = new FulfilmentFileShareService(fakefileShareServiceConfig, fakefileShareService, fakeazureBlobStorageClient, fakeConfiguration);
        }
        private List<Products> GetProductdetails()
        {
            return new List<Products> {
                            new Products {
                                ProductName = "DE5NOBRK",
                                EditionNumber = 0,
                                UpdateNumbers = new List<int?> {0,1},
                                FileSize = 400
                            }
                        };
        }

        private SearchBatchResponse GetSearchBatchResponse()
        {
            return new SearchBatchResponse()
            {
                Entries = new List<BatchDetail>() {
                    new BatchDetail {
                        BatchId ="63d38bde-5191-4a59-82d5-aa22ca1cc6dc"
                    } },
                Links = new PagingLinks(),
                Count = 0,
                Total = 0
            };
        }

        [Test]
        public async Task WhenRequestQueryFileShareServiceData_ThenReturnsFulfillmentDataResponse()
        {
            A.CallTo(() =>  fakefileShareService.GetBatchInfoBasedOnProducts(A<List<Products>>.Ignored)).Returns(GetSearchBatchResponse());

            var result = await fulfilmentFileShareService.QueryFileShareServiceData(GetProductdetails());

            Assert.IsNotNull(result);
            Assert.IsInstanceOf(typeof(List<FulfillmentDataResponse>), result);
           
            Assert.AreEqual("Received Fulfilment Data Successfully!!!!", "Received Fulfilment Data Successfully!!!!");
        }

        [Test]
        public async Task WhenRequestQueryFileShareServiceData_ThenReturnsFulfillmentDataNullResponse()
        {
            A.CallTo(() => fakefileShareService.GetBatchInfoBasedOnProducts(A<List<Products>>.Ignored)).Returns(GetSearchBatchResponse());

            var result = await fulfilmentFileShareService.QueryFileShareServiceData(null);

            Assert.IsNull(result);
        }

        [Test]
        public async Task WhenRequestUploadFileShareServiceData_ThenReturnsCloudBlockBlobUri()
        {
            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            string uploadFileName = string.Concat(batchId, ".json");
            string containerName = "testContainer";
            string connectionString = "testConnectionstring";

            A.CallTo(() => fakeazureBlobStorageClient.GetCloudBlockBlob(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(new CloudBlockBlob(new System.Uri("http://tempuri.org/blob")));

            var result = await fulfilmentFileShareService.UploadFileShareServiceData(uploadFileName,new List<FulfillmentDataResponse>(), connectionString, containerName);

            Assert.AreEqual("http://tempuri.org/blob", result);
        }

        [Test]
        public void WhenRequestDownloadFileShareServiceFiles_ThenReturnsFileTospecificPath()
        {
            var message = new SalesCatalogueServiceResponseQueueMessage() { 
                BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc"
            };
            var fulfillmentDataResponses = new List<FulfillmentDataResponse>() {
                new FulfillmentDataResponse{ BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "Demo", UpdateNumber = 3, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" } }
            };
            var result = fulfilmentFileShareService.DownloadFileShareServiceFiles(message, fulfillmentDataResponses);
            Assert.IsNotNull(result);
        }

        [Test]
        public void WhenRequestDownloadFileShareServiceFiles_ThenReturnsNoFileTospecificPath()
        {
            var message = new SalesCatalogueServiceResponseQueueMessage()
            {
                BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc"
            };
            var fulfillmentDataResponses = new List<FulfillmentDataResponse>();
            var result = fulfilmentFileShareService.DownloadFileShareServiceFiles(message, fulfillmentDataResponses);
            Assert.IsNotNull(result);
        }
    }
}
