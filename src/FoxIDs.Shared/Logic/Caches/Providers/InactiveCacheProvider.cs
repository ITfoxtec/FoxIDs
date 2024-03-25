using System.Threading.Tasks;

namespace FoxIDs.Logic.Caches.Providers
{
    public class InactiveCacheProvider : ICacheProvider, IDataCacheProvider
    {
        public ValueTask DeleteAsync(string key) => ValueTask.CompletedTask;

        public ValueTask<string> GetAsync(string key) => ValueTask.FromResult<string>(null);
        public ValueTask<long> GetNumberAsync(string key) => ValueTask.FromResult<long>(0);

        public ValueTask<bool> ExistsAsync(string key) => ValueTask.FromResult(false);

        public ValueTask SetAsync(string key, string value, int lifetime) => ValueTask.CompletedTask;
        public ValueTask SetFlagAsync(string key, int lifetime) => ValueTask.CompletedTask;

        public ValueTask<long> IncrementNumberAsync(string key, int? lifetime = null) => ValueTask.FromResult<long>(0);
    }
}