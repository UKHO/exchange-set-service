using Newtonsoft.Json;
using StackExchange.Redis;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public class RedisCache : IRedisCache
    {
        private IDatabase _db;
        public RedisCache(IConnectionMultiplexer muxer)
        {
            _db = muxer.GetDatabase();
        }

        public bool SetCacheData<T>(string key, T value)
        {
            var isSet = _db.StringSet(key, JsonConvert.SerializeObject(value));
            return isSet;
        }

        public T GetCacheData<T>(string key)
        {
            var value = _db.StringGet(key);
            
            if (!string.IsNullOrEmpty(value))
            {
                return JsonConvert.DeserializeObject<T>(value);
            }
            return default;
        }

        public object RemoveData(string key)
        {
            bool _isKeyExist = _db.KeyExists(key);
            if (_isKeyExist == true)
            {
                return _db.KeyDelete(key);
            }
            return false;
        }

    }
}
