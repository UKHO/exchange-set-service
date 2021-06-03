using Microsoft.Extensions.Configuration;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public class TestConfiguration
    {
        protected IConfigurationRoot ConfigurationRoot;
        public string EssBaseAddress;
        public static string FakeTokenPrivateKey;
        public string ExchangeSetFileName;
        public string EssStorageAccountConnectionString;
        public EssAuthorizationTokenConfiguration EssAuthorizationConfig = new EssAuthorizationTokenConfiguration();
        public class EssAuthorizationTokenConfiguration
        {
            public string MicrosoftOnlineLoginUrl { get; set; }
            public string TenantId { get; set; }
            public string AutoTestClientId { get; set; }
            public string AutoTestClientSecret { get; set; }
            public string AutoTestClientIdNoAuth { get; set; }
            public string AutoTestClientSecretNoAuth { get; set; }
            public string EssClientId { get; set; }
            public bool IsRunningOnLocalMachine { get; set; }
        }

        public TestConfiguration()
        {
            ConfigurationRoot = new ConfigurationBuilder()
                                .AddJsonFile("appsettings.json", false)
                                .Build();
            EssStorageAccountConnectionString = ConfigurationRoot.GetSection("EssStorageAccountConnectionString").Value;
            EssBaseAddress = ConfigurationRoot.GetSection("EssApiUrl").Value;
            ExchangeSetFileName= ConfigurationRoot.GetSection("ExchangeSetFileName").Value;
            FakeTokenPrivateKey = ConfigurationRoot.GetSection("FakeTokenPrivateKey").Value;
            ConfigurationRoot.Bind("EssAuthorizationConfiguration", EssAuthorizationConfig);
            
        }
    }
}