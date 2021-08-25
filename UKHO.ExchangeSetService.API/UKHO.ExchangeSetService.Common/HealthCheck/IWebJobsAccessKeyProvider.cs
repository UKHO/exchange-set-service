namespace UKHO.ExchangeSetService.Common.HealthCheck
{
    public interface IWebJobsAccessKeyProvider
    {
        public string GetWebJobsAccessKey(string keyName);
    }
}
