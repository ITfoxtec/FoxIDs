using System.Threading.Tasks;

namespace FoxIDs.Logic.Caches.Providers
{
    public interface ICacheProvider
    {
        public ValueTask DeleteAsync(string key);

        public ValueTask<string> GetAsync(string key);
        public ValueTask<long> GetNumberAsync(string key);

        public ValueTask<bool> ExistsAsync(string key);

        public ValueTask SetAsync(string key, string value, int lifetime);
        public ValueTask SetFlagAsync(string key, int lifetime);

        public ValueTask<long> IncrementNumberAsync(string key, int? lifetime = null);
    }
}