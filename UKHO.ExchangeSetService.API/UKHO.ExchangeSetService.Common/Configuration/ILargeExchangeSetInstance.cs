namespace UKHO.ExchangeSetService.Common.Configuration
{
    public interface ILargeExchangeSetInstance
    {
        int GetCurrentInstaceCount();
        int GetInstanceCount();
        void ResetInstanceCount();
    }
}
