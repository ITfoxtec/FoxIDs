using System;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace FoxIDs.Logic.Caches.Providers
{
    public class RedisCacheProvider(IConnectionMultiplexer redisConnectionMultiplexer) : ICacheProvider, IDataCacheProvider
    {
        public async ValueTask DeleteAsync(string key)
        {
            _ = await redisConnectionMultiplexer.GetDatabase().KeyDeleteAsync(key);
        }            

        public async ValueTask<bool> ExistsAsync(string key) =>
            await redisConnectionMultiplexer.GetDatabase().KeyExistsAsync(key);

        public async ValueTask<string> GetAsync(string key) =>
            await redisConnectionMultiplexer.GetDatabase().StringGetAsync(key);

        public async ValueTask<long> GetNumberAsync(string key)
        {
            var valueString = await GetAsync(key);
            if (long.TryParse(valueString, out var number))
            {
                return number;
            }
            else
            {
                return 0;
            }
        }

        public async ValueTask SetAsync(string key, string value, int lifetime)
        {
            _ = await redisConnectionMultiplexer.GetDatabase().StringSetAsync(key, value, TimeSpan.FromSeconds(lifetime));
        }            

        public async ValueTask SetFlagAsync(string key, int lifetime)
        {
            await SetAsync(key, true.ToString(), lifetime);
        }

        public async ValueTask<long> IncrementNumberAsync(string key, int? lifetime = null)
        {
            var db = redisConnectionMultiplexer.GetDatabase();
            var loginCount = await db.StringIncrementAsync(key);
            if (lifetime.HasValue)
            {
                await db.KeyExpireAsync(key, TimeSpan.FromSeconds(lifetime.Value));
            }
            return loginCount;
        }
    }
}