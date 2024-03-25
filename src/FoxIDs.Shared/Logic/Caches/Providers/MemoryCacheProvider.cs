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
            key = GetKey(key);

            memoryCache.Remove(key);
            return ValueTask.CompletedTask;
        }

        public ValueTask<string> GetAsync(string key) => ValueTask.FromResult(memoryCache.Get(GetKey(key)) as string);
        public ValueTask<long> GetNumberAsync(string key)
        {
            key = GetKey(key);

            var number = GetNumber(key);
            return ValueTask.FromResult(number);
        }

        private long GetNumber(string key)
        {
            key = GetKey(key);

            var value = memoryCache.Get(key) as string;
            if (!long.TryParse(value, out var number))
            {
                number = 0;
            }

            return number;
        }

        public ValueTask<bool> ExistsAsync(string key) => ValueTask.FromResult(memoryCache.TryGetValue(GetKey(key), out var value));

        public ValueTask SetAsync(string key, string value, int lifetime)
        {
            key = GetKey(key);

            _ = memoryCache.Set(key, value, DateTimeOffset.Now.AddSeconds(lifetime));
            return ValueTask.CompletedTask;
        }

        public ValueTask SetFlagAsync(string key, int lifetime)
        {
            key = GetKey(key);

            return SetAsync(key, true.ToString(), lifetime);
        }

        public ValueTask<long> IncrementNumberAsync(string key, int? lifetime = null)
        {
            key = GetKey(key);

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

        private string GetKey(string key) => $"{Constants.Models.DataType.Cache}:{key}";
    }
}