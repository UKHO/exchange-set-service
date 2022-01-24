using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.AzureADB2C;

namespace UKHO.ExchangeSetService.Common.UnitTests.Helpers
{
    public class AzureAdB2CHelperTest
    {
        private ILogger<AzureAdB2CHelper> fakeLogger;
        private IOptions<AzureAdB2CConfiguration> fakeAzureAdB2CConfig;
        private IOptions<AzureADConfiguration> fakeAzureAdConfig;
        private IAzureAdB2CHelper fakeAzureAdB2CHelper;

        [SetUp]
        public void Setup()
        {
            fakeLogger = A.Fake<ILogger<AzureAdB2CHelper>>();
            fakeAzureAdB2CConfig = A.Fake<IOptions<AzureAdB2CConfiguration>>();
            fakeAzureAdConfig = A.Fake<IOptions<AzureADConfiguration>>();
            fakeAzureAdB2CConfig.Value.ClientId = "9bca00f0-20d9-4b38-88eb-c7aff6b0f571";
            fakeAzureAdB2CConfig.Value.Instance = "https://gk.microsoft.com/";
            fakeAzureAdB2CConfig.Value.TenantId = "0b29766b-896f-46df-8f1a-122d7c000d91";
            fakeAzureAdConfig.Value.MicrosoftOnlineLoginUrl = "https://www.microsoft.com/";

            fakeAzureAdB2CHelper = new AzureAdB2CHelper(fakeLogger, fakeAzureAdB2CConfig, fakeAzureAdConfig);
        }

        [Test]
        public void WhenAdTokenRequestedIsAzureB2CUser_ThenReturnsResponseFalse()
        {
            var azureADToken = GetAzureADToken();
            var result = fakeAzureAdB2CHelper.IsAzureB2CUser(azureADToken, null);
            Assert.IsFalse(result);
        }

        [Test]
        public void WhenAdB2CTokenRequestedIsAzureB2CUser_ThenReturnsResponseTrue()
        {
            var azureAdB2CToken = GetAzureAdB2CToken();
            var result = fakeAzureAdB2CHelper.IsAzureB2CUser(azureAdB2CToken, null);
            Assert.IsTrue(result);
        }

        [Test]
        public void WhenB2CTokenRequestedIsAzureB2CUser_ThenReturnsResponseTrue()
        {
            var azureB2CToken = GetAzureB2CToken();
            var result = fakeAzureAdB2CHelper.IsAzureB2CUser(azureB2CToken, null);
            Assert.IsTrue(result);
        }        

        #region AzureADB2CToken
        private AzureAdB2C GetAzureADToken()
        {
            return new AzureAdB2C()
            {
                AudToken = string.Empty,
                IssToken = string.Empty
            };
        }

        private AzureAdB2C GetAzureB2CToken()
        {
            return new AzureAdB2C()
            {
                AudToken = "9bca00f0-20d9-4b38-88eb-c7aff6b0f571",
                IssToken = "https://gk.microsoft.com/0b29766b-896f-46df-8f1a-122d7c000d91/v2.0/"
            };
        }

        private AzureAdB2C GetAzureAdB2CToken()
        {
            return new AzureAdB2C()
            {
                AudToken = "9bca00f0-20d9-4b38-88eb-c7aff6b0f571",
                IssToken = "https://www.microsoft.com/0b29766b-896f-46df-8f1a-122d7c000d91/v2.0"
            };
        }
        #endregion AzureB2CToken
    }
}
