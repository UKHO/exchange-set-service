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
        public string ExchangeSetFileFolder;
        public string EncRootFolder;
        public string EncHomeFolder;
        public int FileDownloadWaitTime { get; set; }
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
            ExchangeSetFileFolder = ConfigurationRoot.GetSection("ExchangeSetFileFolder").Value;
            EncRootFolder = ConfigurationRoot.GetSection("EncRootFolder").Value;
            EncHomeFolder = ConfigurationRoot["HOME"];
            FileDownloadWaitTime = ConfigurationRoot.GetSection("FileDownloadWaitTime").Value != null ? int.Parse(ConfigurationRoot.GetSection("FileDownloadWaitTime").Value) : 0;
            EssBaseAddress = ConfigurationRoot.GetSection("EssApiUrl").Value;
            ExchangeSetFileName= ConfigurationRoot.GetSection("ExchangeSetFileName").Value;
            FakeTokenPrivateKey = ConfigurationRoot.GetSection("FakeTokenPrivateKey").Value;
            ConfigurationRoot.Bind("EssAuthorizationConfiguration", EssAuthorizationConfig);
            
        }
    }
}