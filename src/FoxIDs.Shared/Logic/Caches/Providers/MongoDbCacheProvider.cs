using FoxIDs.Repository;
using System;
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

        public ValueTask DeleteAsync(string key)
        {
            throw new NotImplementedException();
        }

        public ValueTask<bool> ExistsAsync(string key)
        {
            throw new NotImplementedException();
        }

        public ValueTask<string> GetAsync(string key)
        {
            throw new NotImplementedException();
        }

        public ValueTask<long> GetNumberAsync(string key)
        {
            throw new NotImplementedException();
        }

        public ValueTask<long> IncrementNumberAsync(string key, int? lifetime = null)
        {
            throw new NotImplementedException();
        }

        public ValueTask SetAsync(string key, string value, int lifetime)
        {
            throw new NotImplementedException();
        }

        public ValueTask SetFlagAsync(string key, int lifetime)
        {
            throw new NotImplementedException();
        }
    }
}