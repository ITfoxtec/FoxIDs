using FoxIDs.Models;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using System.Threading.Tasks;

namespace FoxIDs.Logic.Caches.Providers
{
    public class FileCacheProvider : ICacheProvider
    {
        private readonly FileDataRepository fileDataRepository;

        public FileCacheProvider(FileDataRepository fileDataRepository)
        {
            this.fileDataRepository = fileDataRepository;
        }

        public async ValueTask DeleteAsync(string key)
        {
            var id = await GetIdAsync(key);
            await fileDataRepository.DeleteAsync(id, GetPartitionId(), required: false);
        }

        public async ValueTask<string> GetAsync(string key)
        {
            var id = await GetIdAsync(key);
            var item = await fileDataRepository.GetAsync(id, GetPartitionId(), required: false);
            if (item == null)
            {
                return null;
            }
            return item.DataJsonToObject<CacheData>().Data;
        }
            
        public async ValueTask<long> GetNumberAsync(string key)
        {
            var id = await GetIdAsync(key);
            return await GetNumberInternalAsync(id);
        }

        private async Task<long> GetNumberInternalAsync(string id)
        {
            var item = await fileDataRepository.GetAsync(id, GetPartitionId(), required: false);
            if (item == null)
            {
                return 0;
            }
            if (!long.TryParse(item.DataJsonToObject<CacheData>().Data, out var number))
            {
                number = 0;
            }
            return number;
        }

        public async ValueTask<bool> ExistsAsync(string key)
        {
            var id = await GetIdAsync(key);
            return await fileDataRepository.ExistsAsync(id, GetPartitionId());
        }

        public async ValueTask SetAsync(string key, string value, int lifetime)
        {
            var id = await GetIdAsync(key);
            await fileDataRepository.SaveAsync(id, GetPartitionId(), new CacheTtlData { Id = key, PartitionId = GetPartitionId(), Data = value, TimeToLive = lifetime }.ToJson());
        }

        public async ValueTask SetFlagAsync(string key, int lifetime)
        {
            await SetAsync(key, true.ToString(), lifetime);
        }

        public async ValueTask<long> IncrementNumberAsync(string key, int? lifetime = null)
        {
            var id = await GetIdAsync(key);

            var number = await GetNumberInternalAsync(id);
            number++;
            if (lifetime.HasValue)
            {
                await fileDataRepository.SaveAsync(id, GetPartitionId(), new CacheTtlData { Id = key, PartitionId = GetPartitionId(), Data = number.ToString(), TimeToLive = lifetime.Value }.ToJson());
            }
            else
            {
                await fileDataRepository.SaveAsync(id, GetPartitionId(), new CacheData { Id = key, PartitionId = GetPartitionId(), Data = number.ToString() }.ToJson());
            }
            return number;
        }

        private async Task<string> GetIdAsync(string key) => $"{Constants.Models.DataType.Cache}:{await key.HashIdStringAsync()}";

        private string GetPartitionId() => Constants.Models.DataType.Cache;
    }
}