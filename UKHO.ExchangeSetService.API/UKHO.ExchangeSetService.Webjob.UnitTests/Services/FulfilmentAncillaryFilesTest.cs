using Microsoft.Extensions.Options;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.FulfilmentService.Services;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Services
{
    [TestFixture]
    public class FulfilmentAncillaryFilesTest
    {
        public IOptions<FileShareServiceConfiguration> fileShareServiceConfig;
        public FulfilmentAncillaryFiles fulfilmentAncillaryFiles;

        [SetUp]
        public void Setup()
        {
            fileShareServiceConfig = Options.Create(new FileShareServiceConfiguration()
            {
                BaseUrl = "http://tempuri.org",
                CellName = "DE260001",
                EditionNumber = "1",
                Limit = 10,
                Start = 0,
                ProductCode = "AVCS",
                ProductLimit = 4,
                UpdateNumber = "0",
                UpdateNumberLimit = 10,
                ParallelSearchTaskCount = 10,
                EncRoot = "ENC_ROOT",
                ExchangeSetFileFolder = "V01X01",
                ReadMeFileName = "ReadMe.txt",
                CatalogFileName = "CATALOG.031"
            });

            fulfilmentAncillaryFiles = new FulfilmentAncillaryFiles(fileShareServiceConfig);
        }

        [Test]
        public async Task WhenValidCatalogFileCreated_ThenReturnTrueReponse()
        {
            var fulfilmentDataResponses = new List<FulfilmentDataResponse>() {
                new FulfilmentDataResponse{ BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "Demo", UpdateNumber = 3, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files= new List<BatchFile>(){ new BatchFile { Filename = "test.txt", FileSize = 400, MimeType = "text/plain", Links = new Links { Get = new Link { Href = "" }}}}}
            };
            ////A.CallTo(() => fakeFulfilmentAncillaryFiles.CreateCatalogFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, fulfilmentDataResponses)).Returns(true);

            string batchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc";
            string exchangeSetRootPath = @"C:\\HOME";
            string correlationId = "a6670458-9bbc-4b52-95a2-d1f50fe9e3ae";
            var response = await fulfilmentAncillaryFiles.CreateCatalogFile(batchId, exchangeSetRootPath, correlationId, fulfilmentDataResponses);
            Assert.AreEqual(true, response);
        }

        [Test]
        public async Task WhenNoCatalogFileCreated_ThenReturnFalseReponse()
        {
            string batchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc";
            string exchangeSetRootPath = @"C:\\HOME";
            string correlationId = "a6670458-9bbc-4b52-95a2-d1f50fe9e3ae";
            var response = await fulfilmentAncillaryFiles.CreateCatalogFile(batchId, exchangeSetRootPath, correlationId, null);
            Assert.AreEqual(false, response);
        }
    }
}
