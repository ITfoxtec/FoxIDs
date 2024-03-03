using System;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace FoxIDs.Logic;

public class MemoryCacheProvider() : ICacheProvider
{
    public Task<bool> DeleteAsync(string key) =>
        Task.FromResult(MemoryCache.Default.Remove(key) != null);

    public Task<string> GetAsync(string key) =>
        Task.FromResult(MemoryCache.Default.Get(key) as string);

    public Task<bool> SetAsync(string key, string value, int lifetime)
    {
        MemoryCache.Default.Set(key, value, DateTimeOffset.Now.AddSeconds(lifetime));
        return Task.FromResult(true);
    }
}