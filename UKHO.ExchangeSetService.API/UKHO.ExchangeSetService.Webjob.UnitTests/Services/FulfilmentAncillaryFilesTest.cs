using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.FulfilmentService.Services;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Services
{
    [TestFixture]
    public class FulfilmentAncillaryFilesTest
    {
        public IOptions<FileShareServiceConfiguration> fakefileShareServiceConfig;
        public IFileSystemHelper fakeFileSystemHelper;
        public FulfilmentAncillaryFiles fulfilmentAncillaryFiles;
        public ILogger<FulfilmentDataService> fakeLogger;
        public string fakeBatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
        public string fakeExchangeSetPath = string.Empty;

        [SetUp]
        public void Setup()
        {
            fakefileShareServiceConfig = Options.Create(new FileShareServiceConfiguration()
            { SerialFileName = "TEST.ENC" });
            fakeFileSystemHelper = A.Fake<IFileSystemHelper>();
            fakeLogger = A.Fake<ILogger<FulfilmentDataService>>();
            fulfilmentAncillaryFiles = new FulfilmentAncillaryFiles(fakefileShareServiceConfig, fakeFileSystemHelper, fakeLogger);
        }

        [Test]
        public async Task WhenInvalidCreateSerialEncFileRequest_ThenReturnFalseResponse()
        {
            var response = await fulfilmentAncillaryFiles.CreateSerialEncFile(fakeBatchId, fakeExchangeSetPath, null);

            Assert.AreEqual(false, response);
        }
        
        [Test]
        public async Task WhenValidCreateSerialEncFileRequest_ThenReturnTrueResponse()
        {
            fakeExchangeSetPath = @"C:\\HOME";
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored));
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).Returns(true);

            var response = await fulfilmentAncillaryFiles.CreateSerialEncFile(fakeBatchId, fakeExchangeSetPath, null);

            Assert.AreEqual(true, response);
        }
    }
}
