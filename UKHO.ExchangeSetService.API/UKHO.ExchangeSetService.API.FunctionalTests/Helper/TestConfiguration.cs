
using Microsoft.Extensions.Configuration;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public class TestConfiguration
    {

        protected IConfigurationRoot ConfigurationRoot;
        public string EssBaseAddress;
        public TestConfiguration()
        {
            ConfigurationRoot = new ConfigurationBuilder()
                                .AddJsonFile("appSettings.json", false)
                                .Build();

            EssBaseAddress = ConfigurationRoot.GetSection("EssApiUrl").Value;
        }
    }
}
