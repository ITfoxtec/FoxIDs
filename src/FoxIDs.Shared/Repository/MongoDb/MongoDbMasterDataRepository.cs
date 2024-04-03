using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FoxIDs.Repository.MongoDb
{
    public class MongoDbMasterDataRepository : MasterDataRepositoryBase
    {
        private readonly MongoDbRepositoryClient mongoDbRepositoryClient;

        public MongoDbMasterDataRepository(MongoDbRepositoryClient mongoDbRepositoryClient)
        {
            this.mongoDbRepositoryClient = mongoDbRepositoryClient;
        }

        public override ValueTask<int> CountAsync<T>(Expression<Func<T, bool>> whereQuery = null)
        {
            throw new NotImplementedException();
        }

        public override ValueTask CreateAsync<T>(T item)
        {
            throw new NotImplementedException();
        }

        public override ValueTask DeleteAsync<T>(T item)
        {
            throw new NotImplementedException();
        }

        public override ValueTask<T> DeleteAsync<T>(string id)
        {
            throw new NotImplementedException();
        }

        public override ValueTask DeleteBulkAsync<T>(List<string> ids)
        {
            throw new NotImplementedException();
        }

        public override ValueTask<bool> ExistsAsync<T>(string id)
        {
            throw new NotImplementedException();
        }

        public override ValueTask<T> GetAsync<T>(string id, bool required = true)
        {
            throw new NotImplementedException();
        }

        public override ValueTask<HashSet<T>> GetListAsync<T>(Expression<Func<T, bool>> whereQuery = null, int maxItemCount = 50)
        {
            throw new NotImplementedException();
        }

        public override ValueTask SaveAsync<T>(T item)
        {
            throw new NotImplementedException();
        }

        public override ValueTask SaveBulkAsync<T>(List<T> items)
        {
            throw new NotImplementedException();
        }

        public override ValueTask UpdateAsync<T>(T item)
        {
            throw new NotImplementedException();
        }
    }
}
