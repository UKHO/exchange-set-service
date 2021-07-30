namespace UKHO.ExchangeSetService.Common.Configuration
{
    public interface ISmallExchangeSetInstance
    {
        int GetCurrentInstanceNumber();
        int GetInstanceNumber(int maxInstanceCount);
    }
}
