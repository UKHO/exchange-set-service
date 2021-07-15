namespace UKHO.ExchangeSetService.Common.Configuration
{
    public interface ISmallExchangeSetInstance
    {
        int GetCurrentInstaceCount();
        int GetInstanceCount();
        void ResetInstanceCount();
    }
}
