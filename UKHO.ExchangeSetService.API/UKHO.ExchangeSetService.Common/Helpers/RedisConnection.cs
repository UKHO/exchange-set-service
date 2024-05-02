using StackExchange.Redis;
using System;
using UKHO.ExchangeSetService.Common.Configuration;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public class RedisConnection
    {
        public RedisConnection(RedisConfiguration redisConfig)
        {
            RedisConnection.lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
            {
                return ConnectionMultiplexer.Connect(redisConfig.CacheConnectionString);
            });
        }
        private static Lazy<ConnectionMultiplexer> lazyConnection;
        public static ConnectionMultiplexer Connection
        {
            get
            {
                return lazyConnection.Value;
            }
        }
    }
}
