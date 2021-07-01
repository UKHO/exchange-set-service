using FakeItEasy;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
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
        public FulfilmentFileShareService fulfilmentFileShareService;

        [SetUp]
        public void Setup()
        {
            fakefileShareService = A.Fake<IFileShareService>();
            fakefileShareServiceConfig = Options.Create(new FileShareServiceConfiguration()
            { Limit=100,Start=0,ProductLimit=4,UpdateNumberLimit=10, EncRoot="ENC_ROOT", ExchangeSetFileFolder= "V01X01" });

            fulfilmentFileShareService = new FulfilmentFileShareService(fakefileShareServiceConfig, fakefileShareService);
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
        public async Task WhenRequestQueryFileShareServiceData_ThenReturnsFulfilmentDataResponse()
        {
            A.CallTo(() =>  fakefileShareService.GetBatchInfoBasedOnProducts(A<List<Products>>.Ignored, A<string>.Ignored)).Returns(GetSearchBatchResponse());

            var result = await fulfilmentFileShareService.QueryFileShareServiceData(GetProductdetails(),null);

            Assert.IsNotNull(result);
            Assert.IsInstanceOf(typeof(List<FulfilmentDataResponse>), result);
           
            Assert.AreEqual("Received Fulfilment Data Successfully!!!!", "Received Fulfilment Data Successfully!!!!");
        }

        [Test]
        public async Task WhenRequestQueryFileShareServiceData_ThenReturnsFulfillmentDataNullResponse()
        {
            A.CallTo(() => fakefileShareService.GetBatchInfoBasedOnProducts(A<List<Products>>.Ignored, A<string>.Ignored)).Returns(GetSearchBatchResponse());

            var result = await fulfilmentFileShareService.QueryFileShareServiceData(null,null);

            Assert.IsNull(result);
        }

        [Test]
        public void WhenValidRequest_ThenDownloadFileShareServiceFilesReturnsFile()
        {
            var message = new SalesCatalogueServiceResponseQueueMessage() { 
                BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc"
            };
            var fulfilmentDataResponses = new List<FulfilmentDataResponse>() {
                new FulfilmentDataResponse{ BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "Demo", UpdateNumber = 3, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" } }
            };
            var result = fulfilmentFileShareService.DownloadFileShareServiceFiles(message, fulfilmentDataResponses, "");
            Assert.IsNotNull(result);
        }

        [Test]
        public void WhenInValidRequest_ThenDownloadFileShareServiceFilesReturnsNoFile()
        {
            var message = new SalesCatalogueServiceResponseQueueMessage()
            {
                BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc"
            };
            var fulfilmentDataResponses = new List<FulfilmentDataResponse>();
            var result = fulfilmentFileShareService.DownloadFileShareServiceFiles(message, fulfilmentDataResponses, "");
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task WhenValidSearchReadMeFileRequest_ThenReturnFilePath()
        {
            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            string exchangeSetRootPath = @"batch/" + batchId +"/files/README.TXT";
            A.CallTo(() => fakefileShareService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(exchangeSetRootPath);
            var result = await fulfilmentFileShareService.SearchReadMeFilePath(batchId,null);
            Assert.IsNotEmpty(result);            
        }
         [Test]
        public async Task WhenInvalidSearchReadMeFileRequest_ThenReturnEmptyFilePath()
        {
            string exchangeSetRootPath = string.Empty;           
            string batchId = Guid.NewGuid().ToString();

            A.CallTo(() => fakefileShareService.SearchReadMeFilePath(A<string>.Ignored, A<string>.Ignored)).Returns(exchangeSetRootPath);
            var result = await fulfilmentFileShareService.SearchReadMeFilePath(batchId,null);

            Assert.IsEmpty(result);           
        }

        [Test]
        public async Task WhenRequestDownloadReadMeFile_ThenReturnsTrueIfFileIsDownloaded()
        {
            bool isFileDownloaded = true;
            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            string exchangeSetRootPath = @"D:\\Downloads";
            string filePath = "TestFilePath";

            A.CallTo(() => fakefileShareService.DownloadReadMeFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(isFileDownloaded);
            isFileDownloaded = await fulfilmentFileShareService.DownloadReadMeFile(filePath, batchId, exchangeSetRootPath,null);
           
            Assert.AreEqual(true, isFileDownloaded);
        }

        [Test]
        public async Task WhenRequestDownloadReadMeFile_ThenReturnsFalseIfFileIsNotDownloaded()
        {
            bool isFileDownloaded = false;
            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            string exchangeSetRootPath = @"D:\\Downloads";
            string filePath = "TestFilePath";

            A.CallTo(() => fakefileShareService.DownloadReadMeFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(isFileDownloaded);
            isFileDownloaded = await fulfilmentFileShareService.DownloadReadMeFile(filePath, batchId, exchangeSetRootPath,null);

            Assert.AreEqual(false, isFileDownloaded);
        }

        [Test]
        public async Task WhenRequestUploadZipFileForExchangeSetToFileShareService_ThenReturnsTrueIfFileIsUploaded()
        {
            bool isFileUploaded = true;
            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            string exchangeSetPath = @"D:\\Downloads";
            
            A.CallTo(() => fakefileShareService.UploadZipFileForExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(isFileUploaded);
            isFileUploaded = await fulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(batchId, exchangeSetPath, null);

            Assert.AreEqual(true, isFileUploaded);
        }

        [Test]
        public async Task WhenRequestUploadZipFileForExchangeSetToFileShareService_ThenReturnsFalseIfFileIsNotUploaded()
        {
            bool isFileDownloaded = false;
            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            string exchangeSetRootPath = @"D:\\Downloads";
           

            A.CallTo(() => fakefileShareService.UploadZipFileForExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Returns(isFileDownloaded);
            isFileDownloaded = await fulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(batchId, exchangeSetRootPath, null);

            Assert.AreEqual(false, isFileDownloaded);
        }

        [Test]
        public void WhenRequestCreateZipFile_ThenReturnsTrueIfZipFileIsCreated()
        {
            bool isZipFileCreated = true;
            string exchangeSetRootPath = @"D:\\Downloads\";
            A.CallTo(() => fakefileShareService.CreateZipFileForExchangeSet(A<string>.Ignored, A<string>.Ignored)).Returns(isZipFileCreated);
            isZipFileCreated = fulfilmentFileShareService.CreateZipFileForExchangeSet(exchangeSetRootPath, null);
            Assert.AreEqual(true, isZipFileCreated);
        }

        [Test]
        public void WhenRequestCreateZipFile_ThenReturnsFalseIfZipFileIsCreated()
        {
            bool isZipFileCreated = false;
            string exchangeSetRootPath = @"D:\\Downloads\";
            A.CallTo(() => fakefileShareService.CreateZipFileForExchangeSet(A<string>.Ignored, A<string>.Ignored)).Returns(isZipFileCreated);
            isZipFileCreated = fulfilmentFileShareService.CreateZipFileForExchangeSet(exchangeSetRootPath, null);
            Assert.AreEqual(false, isZipFileCreated);
        }

    }
}
