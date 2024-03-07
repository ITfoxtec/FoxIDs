using System;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace FoxIDs.Logic;

public class RedisCacheProvider(IConnectionMultiplexer redisConnectionMultiplexer) : IDistributedCacheProvider
{
    public async Task<bool> DeleteAsync(string key) =>
        await redisConnectionMultiplexer.GetDatabase().KeyDeleteAsync(key);

    public async Task<bool> ExistsAsync(string key) =>
        await redisConnectionMultiplexer.GetDatabase().KeyExistsAsync(key);

    public async Task<string> GetAsync(string key) =>
        await redisConnectionMultiplexer.GetDatabase().StringGetAsync(key);

    public async Task<long> GetNumberAsync(string key)
    {
        var valueString = await GetAsync(key);
        if(long.TryParse(valueString, out var number))
        { 
            return number; 
        }
        else 
        {
            return 0; 
        }
    }

    public async Task<bool> SetAsync(string key, string value, int lifetime) =>
        await redisConnectionMultiplexer.GetDatabase().StringSetAsync(key, value, TimeSpan.FromSeconds(lifetime));

    public async Task<bool> SetFlagAsync(string key, int lifetime)
    {
        return await SetAsync(key, true.ToString(), lifetime);
    }

    public async Task<long> IncrementNumberAsync(string key, int? lifetime = null)
    {
        var db = redisConnectionMultiplexer.GetDatabase();
        var loginCount = await db.StringIncrementAsync(key);
        if(lifetime.HasValue)
        {
            await db.KeyExpireAsync(key, TimeSpan.FromSeconds(lifetime.Value));
        }
        return loginCount;
    }
}