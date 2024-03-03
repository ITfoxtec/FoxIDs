using System.Threading.Tasks;

namespace FoxIDs.Logic;

public interface ICacheProvider
{
    public Task<bool> DeleteAsync(string key);
    public Task<string> GetAsync(string key);
    public Task<bool> SetAsync(string key, string value, int lifetime);
}