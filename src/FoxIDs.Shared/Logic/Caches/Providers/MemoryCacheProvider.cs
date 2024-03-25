using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Logic.Caches.Providers
{
    public class MemoryCacheProvider : ICacheProvider
    {
        private readonly IMemoryCache memoryCache;

        public MemoryCacheProvider(IMemoryCache memoryCache)
        {
            this.memoryCache = memoryCache;
        }

        public ValueTask DeleteAsync(string key)
        {
            memoryCache.Remove(key);
            return ValueTask.CompletedTask;
        }

        public ValueTask<string> GetAsync(string key) => ValueTask.FromResult(memoryCache.Get(key) as string);
        public ValueTask<long> GetNumberAsync(string key)
        {
            var number = GetNumber(key);
            return ValueTask.FromResult(number);
        }

        private long GetNumber(string key)
        {
            var value = memoryCache.Get(key) as string;
            if (!long.TryParse(value, out var number))
            {
                number = 0;
            }

            return number;
        }

        public ValueTask<bool> ExistsAsync(string key) => ValueTask.FromResult(memoryCache.TryGetValue(key, out var value));

        public ValueTask SetAsync(string key, string value, int lifetime)
        {
            _ = memoryCache.Set(key, value, DateTimeOffset.Now.AddSeconds(lifetime));
            return ValueTask.CompletedTask;
        }

        public ValueTask SetFlagAsync(string key, int lifetime)
        {
            return SetAsync(key, true.ToString(), lifetime);
        }

        public ValueTask<long> IncrementNumberAsync(string key, int? lifetime = null)
        {
            var number = GetNumber(key);
            number++;
            if (lifetime.HasValue)
            {
                _ = memoryCache.Set(key, number, DateTimeOffset.Now.AddSeconds(lifetime.Value));
            }
            else
            {
                _ = memoryCache.Set(key, number);
            }
            return ValueTask.FromResult(number);
        }
    }
}