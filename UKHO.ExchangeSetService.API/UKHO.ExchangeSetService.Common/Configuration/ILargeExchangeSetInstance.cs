namespace UKHO.ExchangeSetService.Common.Configuration
{
    public interface ILargeExchangeSetInstance
    {
        int GetCurrentInstanceNumber();
        int GetInstanceNumber(int largeMaxInstanceCount);
    }
}
