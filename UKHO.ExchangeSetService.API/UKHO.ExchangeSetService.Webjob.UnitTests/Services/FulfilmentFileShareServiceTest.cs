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
        public async Task WhenRequestSearchReadMeFilePath_ThenReturnsFilePath()
        {
            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            string exchangeSetRootPath = @"D:\\Downloads";

            A.CallTo(() => fakefileShareService.SearchReadMeFilePath(A<string>.Ignored)).Returns(exchangeSetRootPath);
            var result = await fulfilmentFileShareService.SearchReadMeFilePath(batchId);

            Assert.IsNotNull(result);
            Assert.AreEqual("ReadMe Text file path found", "ReadMe Text file path found");
        }
         [Test]
        public async Task WhenRequestSearchReadMeFilePath_ThenReturnsNullResponse()
        {
            string exchangeSetRootPath = @"D:\\Downloads";

            A.CallTo(() => fakefileShareService.SearchReadMeFilePath(A<string>.Ignored)).Returns(exchangeSetRootPath);
            var result = await fulfilmentFileShareService.SearchReadMeFilePath(null);

            Assert.IsNull(result);           
        }

        [Test]
        public async Task WhenRequestDownloadReadMeFile_ThenReturnsTrueIfFileIsDownloaded()
        {
            bool isFileDownloaded = true;
            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            string exchangeSetRootPath = @"D:\\Downloads";
            string filePath = "TestFilePath";

            A.CallTo(() => fakefileShareService.DownloadReadMeFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(isFileDownloaded);
            isFileDownloaded = await fulfilmentFileShareService.DownloadReadMeFile(filePath, batchId, exchangeSetRootPath);
           
            Assert.AreSame(true, isFileDownloaded);
        }
    }
}
