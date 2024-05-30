using FoxIDs.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Wololo.PgKeyValueDB;

namespace FoxIDs.Logic.Caches.Providers
{
    public class PostgreSqlCacheProvider : ICacheProvider
    {
        private readonly PgKeyValueDB db;

        public PostgreSqlCacheProvider([FromKeyedServices(Constants.Models.DataType.Cache)] PgKeyValueDB db)
        {
            this.db = db;
        }

        public async ValueTask DeleteAsync(string key)
        {
            var id = GetId(key);
            await db.RemoveAsync(id, CachePartitionId);
        }

        public async ValueTask<bool> ExistsAsync(string key)
        {
            var id = GetId(key);
            return await db.ExistsAsync(id, CachePartitionId);
        }

        public async ValueTask<string> GetAsync(string key)
        {
            var id = GetId(key);
            var cachItem = await db.GetAsync<CacheData>(id, CachePartitionId);
            return cachItem?.Data;
        }

        public async ValueTask SetAsync(string key, string value, int lifetime)
        {
            var id = GetId(key);
            var cachItem = new CacheTtlData { Id = id, PartitionId = CachePartitionId, Data = value, TimeToLive = lifetime };
            await db.UpsertAsync(cachItem.Id, cachItem, cachItem.PartitionId, cachItem.ExpireAt);
        }

        public async ValueTask SetFlagAsync(string key, int lifetime)
        {
            await SetAsync(key, true.ToString(), lifetime);
        }

        public async ValueTask<long> GetNumberAsync(string key)
        {
            var id = GetId(key);
            return await GetNumberInternalAsync(id);
        }

        private async ValueTask<long> GetNumberInternalAsync(string id)
        {
            var cachItem = await db.GetAsync<CacheData>(id, CachePartitionId);
            if (cachItem == null)
            {
                return 0;
            }
            if (!long.TryParse(cachItem.Data, out var number))
            {
                number = 0;
            }
            return number;
        }

        public async ValueTask<long> IncrementNumberAsync(string key, int? lifetime = null)
        {
            var id = GetId(key);

            var number = await GetNumberInternalAsync(id);
            number++;

            if (lifetime.HasValue)
            {
                var cachItem = new CacheTtlData { Id = id, PartitionId = CachePartitionId, Data = number.ToString(), TimeToLive = lifetime.Value };
                await db.UpsertAsync(cachItem.Id, cachItem, cachItem.PartitionId, cachItem.ExpireAt);
            }
            else
            {
                var cachItem = new CacheData { Id = id, PartitionId = CachePartitionId, Data = number.ToString() };
                await db.UpsertAsync(cachItem.Id, cachItem, cachItem.PartitionId);
            }
            return number;
        }

        public async Task RemoveAllExpiredAsync()
        {
            _ = await db.RemoveAllExpiredAsync();
        }

        private  string GetId(string key) => $"{CachePartitionId}:{key}";

        private string CachePartitionId => Constants.Models.DataType.Cache;

    }
}