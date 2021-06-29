using Microsoft.Extensions.Options;
using NUnit.Framework;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.FulfilmentService.Services;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Services
{
    [TestFixture]
    public class FulfilmentAncillaryFilesTest
    {
        public IOptions<FileShareServiceConfiguration> fakefileShareServiceConfig;
        public FulfilmentAncillaryFiles fulfilmentAncillaryFiles;

        [SetUp]
        public void Setup()
        {
            fakefileShareServiceConfig = Options.Create(new FileShareServiceConfiguration()
            {  SerialFileName="TEST.ENC" });

            fulfilmentAncillaryFiles = new FulfilmentAncillaryFiles(fakefileShareServiceConfig);
        }

        [Test]
        public async Task WhenRequestCreateSerialEncFile_ThenReturnsFalseIfFilePathIsNull()
        {
            string exchangeSetPath = "";
            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            var response  = await fulfilmentAncillaryFiles.CreateSerialEncFile(batchId, exchangeSetPath, null);
           
            Assert.AreEqual(false, response);
        }

        [Test]
        public async Task WhenRequestCreateSerialEncFile_ThenReturnsTrueIfFilePathIsNotNull()
        {
            string exchangeSetPath = @"C:\\HOME";
            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            var response = await fulfilmentAncillaryFiles.CreateSerialEncFile(batchId, exchangeSetPath, null);

            Assert.AreEqual(true, response);
        }
    }
}
