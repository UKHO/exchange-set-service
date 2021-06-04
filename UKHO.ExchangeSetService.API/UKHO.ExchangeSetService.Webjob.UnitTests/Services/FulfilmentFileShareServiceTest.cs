using FakeItEasy;
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
        public FulfilmentFileShareService fulfilmentFileShareService;

        [SetUp]
        public void Setup()
        {
            fakefileShareService = A.Fake<IFileShareService>();
            fakeazureBlobStorageClient = A.Fake<IAzureBlobStorageClient>();
            fakefileShareServiceConfig = Options.Create(new FileShareServiceConfiguration()
            { Limit=100,Start=0,ProductLimit=4,UpdateNumberLimit=10});

            fulfilmentFileShareService = new FulfilmentFileShareService(fakefileShareServiceConfig, fakefileShareService, fakeazureBlobStorageClient);
        }
        private List<Products> GetProductsdetails()
        {
            return new List<Products> {
                            new Products {
                                ProductName = "productName",
                                EditionNumber = 2,
                                UpdateNumbers = new List<int?> {3,4},
                                FileSize = 400
                            }
                        };
        }

        private SearchBatchResponse GetSearchBatchResponse()
        {
            return new SearchBatchResponse() { 
                Entries = new List<BatchDetail>() { 
                    new BatchDetail {
                        BatchId ="test"
                    } }, 
                Links = new PagingLinks(), 
                Count = 0, 
                Total = 0 
            };
        }

        [Test]
        public async Task WhenRequetsQueryFileShareServiceData_ThenReturnsFulfillmentDataResponse()
        {
            A.CallTo(() =>  fakefileShareService.GetBatchInfoBasedOnProducts(A<List<Products>>.Ignored)).Returns(GetSearchBatchResponse());

            var result = await fulfilmentFileShareService.QueryFileShareServiceData(GetProductsdetails());

            Assert.IsNotNull(result);
            Assert.IsInstanceOf(typeof(List<FulfillmentDataResponse>), result);
           
            Assert.AreEqual("Received Fulfilment Data Successfully!!!!", "Received Fulfilment Data Successfully!!!!");
        }

        [Test]
        public async Task WhenRequetsQueryFileShareServiceData_ThenReturnsFulfillmentDataNullResponse()
        {
            A.CallTo(() => fakefileShareService.GetBatchInfoBasedOnProducts(A<List<Products>>.Ignored)).Returns(GetSearchBatchResponse());

            var result = await fulfilmentFileShareService.QueryFileShareServiceData(null);

            Assert.IsNull(result);
        }

        [Test]
        public async Task WhenRequetsUploadFileShareServiceData_ThenReturnsCloudBlockBlobUri()
        {
            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            string uploadFileName = string.Concat(batchId, ".json");
            string containerName = "testContainer";
            string connectionString = "testConnectionstring";

            A.CallTo(() => fakeazureBlobStorageClient.GetCloudBlockBlob(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(new CloudBlockBlob(new System.Uri("http://tempuri.org/blob")));

            await fulfilmentFileShareService.UploadFileShareServiceData(uploadFileName,new List<FulfillmentDataResponse>(), connectionString, containerName);

            Assert.AreEqual("Received Fulfilment Data Successfully!!!!", "Received Fulfilment Data Successfully!!!!");
        }
    }
}
