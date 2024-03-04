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

    public async Task<bool> SetAsync(string key, string value, int lifetime) =>
        await redisConnectionMultiplexer.GetDatabase().StringSetAsync(key, value, TimeSpan.FromSeconds(lifetime));
}