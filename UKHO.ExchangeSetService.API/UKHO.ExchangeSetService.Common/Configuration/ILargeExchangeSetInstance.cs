namespace UKHO.ExchangeSetService.Common.Configuration
{
    public interface ILargeExchangeSetInstance
    {
        int GetCurrentInstanceCount();
        int GetInstanceCount(int maxInstanceCount);
        void ResetInstanceCount();
    }
}
