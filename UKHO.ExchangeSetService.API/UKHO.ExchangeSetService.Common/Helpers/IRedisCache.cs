

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IRedisCache
    {
        T GetCacheData<T>(string key);
        bool SetCacheData<T>(string key, T value);
        object RemoveData(string key);
    }
}
