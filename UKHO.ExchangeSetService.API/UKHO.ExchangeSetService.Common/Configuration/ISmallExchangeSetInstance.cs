namespace UKHO.ExchangeSetService.Common.Configuration
{
    public interface ISmallExchangeSetInstance
    {
        int GetCurrentInstanceCount();
        int GetInstanceCount(int maxInstanceCount);
        void ResetInstanceCount();
    }
}
