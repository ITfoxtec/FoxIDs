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
            _ = await collection.DeleteOneAsync(f => f.PartitionId.Equals(CachePartitionId) && f.Id.Equals(id));
        }

        public async ValueTask<bool> ExistsAsync(string key)
        {
            var id = GetId(key);
            var collection = mongoDbRepositoryClient.GetCacheCollection<CacheData>();
            var cachItem = await collection.Find(f => f.PartitionId.Equals(CachePartitionId) && f.Id.Equals(id)).FirstOrDefaultAsync();
            return cachItem != null;
        }

        public async ValueTask<string> GetAsync(string key)
        {
            var id = GetId(key);
            var collection = mongoDbRepositoryClient.GetCacheCollection<CacheData>();
            var cachItem = await collection.Find(f => f.PartitionId.Equals(CachePartitionId) && f.Id.Equals(id)).FirstOrDefaultAsync();
            return cachItem?.Data;
        }

        public async ValueTask SetAsync(string key, string value, int lifetime)
        {
            var id = GetId(key);
            var cachItem = new CacheTtlData { Id = id, PartitionId = CachePartitionId, Data = value, TimeToLive = lifetime };

            var collection = mongoDbRepositoryClient.GetCacheCollection<CacheData>();
            Expression<Func<CacheData, bool>> filter = f => f.PartitionId.Equals(cachItem.PartitionId) && f.Id.Equals(cachItem.Id);
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
            (var number, _) = await GetNumberInternalAsync(collection, id);
            return number;
        }

        private async ValueTask<(long number, bool exists)> GetNumberInternalAsync(IMongoCollection<CacheData> collection, string id)
        {
            var cachItem = await collection.Find(f => f.PartitionId.Equals(CachePartitionId) && f.Id.Equals(id)).FirstOrDefaultAsync();
            if (cachItem == null)
            {
                return (0, false);
            }
            if (!long.TryParse(cachItem.Data, out var number))
            {
                number = 0;
            }
            return (number, cachItem != null);
        }

        public async ValueTask<long> IncrementNumberAsync(string key, int? lifetime = null)
        {
            var id = GetId(key);
            var collection = mongoDbRepositoryClient.GetCacheCollection<CacheData>();

            (var number, var exists) = await GetNumberInternalAsync(collection, id);
            number++;

            Expression<Func<CacheData, bool>> filter = f => f.PartitionId.Equals(CachePartitionId) && f.Id.Equals(id);

            if (lifetime.HasValue)
            {
                var cachItem = new CacheTtlData { Id = id, PartitionId = CachePartitionId, Data = number.ToString(), TimeToLive = lifetime.Value };
                if (!exists)
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
                var cachItem = new CacheData { Id = id, PartitionId = CachePartitionId, Data = number.ToString() };
                if (!exists)
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

        private  string GetId(string key) => $"{CachePartitionId}:{key}";

        private string CachePartitionId => Constants.Models.DataType.Cache;

    }
}