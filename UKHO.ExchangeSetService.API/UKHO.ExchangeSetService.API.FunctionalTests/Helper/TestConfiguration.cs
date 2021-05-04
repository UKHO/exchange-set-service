
using Microsoft.Extensions.Configuration;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    /// <summary>
    /// This class is for configuration set up
    /// </summary>
    public class TestConfiguration
    {
        /// <summary>
        /// ConfigurationRoot variable declaire here 
        /// </summary>
        protected IConfigurationRoot ConfigurationRoot;
        /// <summary>
        /// EssBaseAddress variable declaire here 
        /// </summary>
        public string EssBaseAddress;

        /// <summary>
        /// Constructor call here
        /// </summary>
        public TestConfiguration()
        {
            ConfigurationRoot = new ConfigurationBuilder()
                                .AddJsonFile("appSettings.json", false)
                                .Build();

            EssBaseAddress = ConfigurationRoot.GetSection("EssApiUrl").Value;
        }
    }
}
