using FoxIDs.Models;
using FoxIDs.Repository;
using MongoDB.Driver;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FoxIDs.Logic.Caches.Providers
{
    public class MongoDbCacheProvider : ICacheProvider
    {
        private readonly MongoDbRepositoryClient mongoDbRepositoryClient;

        public MongoDbCacheProvider(MongoDbRepositoryClient mongoDbRepositoryClient)
        {
            this.mongoDbRepositoryClient = mongoDbRepositoryClient;
        }

        public async ValueTask DeleteAsync(string key)
        {
            var id = GetId(key);
            var collection = mongoDbRepositoryClient.GetCacheCollection<CacheData>();
            _ = await collection.DeleteOneAsync(f => f.Id.Equals(id, StringComparison.Ordinal));
        }

        public async ValueTask<bool> ExistsAsync(string key)
        {
            var id = GetId(key);
            var collection = mongoDbRepositoryClient.GetCacheCollection<CacheData>();
            var cachItem = await collection.Find(f => f.Id.Equals(id, StringComparison.Ordinal)).FirstOrDefaultAsync();
            return cachItem != null;
        }

        public async ValueTask<string> GetAsync(string key)
        {
            var id = GetId(key);
            var collection = mongoDbRepositoryClient.GetCacheCollection<CacheData>();
            var cachItem = await collection.Find(f => f.Id.Equals(id, StringComparison.Ordinal)).FirstOrDefaultAsync();
            return cachItem?.Data;
        }

        public async ValueTask SetAsync(string key, string value, int lifetime)
        {
            var id = GetId(key);
            var cachItem = new CacheTtlData { Id = id, PartitionId = GetPartitionId(), Data = value, TimeToLive = lifetime };

            var collection = mongoDbRepositoryClient.GetCacheCollection<CacheData>();
            Expression<Func<CacheData, bool>> filter = f => f.Id.Equals(id, StringComparison.Ordinal);
            var data = await collection.Find(filter).FirstOrDefaultAsync();
            if (data == null)
            {
                await collection.InsertOneAsync(cachItem);
            }
            else
            {
                _ = await collection.ReplaceOneAsync(filter, cachItem);
            }
        }

        public async ValueTask SetFlagAsync(string key, int lifetime)
        {
            await SetAsync(key, true.ToString(), lifetime);
        }

        public async ValueTask<long> GetNumberAsync(string key)
        {
            var id = GetId(key);
            var collection = mongoDbRepositoryClient.GetCacheCollection<CacheData>();
            return await GetNumberInternalAsync(collection, id);
        }

        private static async ValueTask<long> GetNumberInternalAsync(IMongoCollection<CacheData> collection, string id)
        {
            var cachItem = await collection.Find(f => f.Id.Equals(id, StringComparison.Ordinal)).FirstOrDefaultAsync();
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
            var collection = mongoDbRepositoryClient.GetCacheCollection<CacheData>();

            var number = await GetNumberInternalAsync(collection, id);
            number++;

            Expression<Func<CacheData, bool>> filter = f => f.Id.Equals(id, StringComparison.Ordinal);
            var data = await collection.Find(filter).FirstOrDefaultAsync();

            if (lifetime.HasValue)
            {
                var cachItem = new CacheTtlData { Id = id, PartitionId = GetPartitionId(), Data = number.ToString(), TimeToLive = lifetime.Value };
                if (data == null)
                {
                    await collection.InsertOneAsync(cachItem);
                }
                else
                {
                    _ = await collection.ReplaceOneAsync(filter, cachItem);
                }
            }
            else
            {
                var cachItem = new CacheData { Id = id, PartitionId = GetPartitionId(), Data = number.ToString() };
                if (data == null)
                {
                    await collection.InsertOneAsync(cachItem);
                }
                else
                {
                    _ = await collection.ReplaceOneAsync(filter, cachItem);
                }
            }
            return number;
        }

        private  string GetId(string key) => $"{GetPartitionId()}:{key}";

        private string GetPartitionId() => Constants.Models.DataType.Cache;

    }
}