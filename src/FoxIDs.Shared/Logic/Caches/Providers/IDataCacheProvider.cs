using System.Threading.Tasks;

namespace FoxIDs.Logic.Caches.Providers
{
    public interface IDataCacheProvider
    {
        public ValueTask DeleteAsync(string key);

        public ValueTask<string> GetAsync(string key);

        public ValueTask SetAsync(string key, string value, int lifetime);
    }
}