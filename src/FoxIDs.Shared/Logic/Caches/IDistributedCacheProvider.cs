using System.Threading.Tasks;

namespace FoxIDs.Logic;

public interface IDistributedCacheProvider
{
    public Task<bool> DeleteAsync(string key);

    public Task<string> GetAsync(string key);
    public Task<long> GetNumberAsync(string key);

    public Task<bool> ExistsAsync(string key);

    public Task<bool> SetAsync(string key, string value, int lifetime);
    public Task<bool> SetFlagAsync(string key, int lifetime);

    public Task<long> IncrementNumberAsync(string key, int? lifetime = null);
}