using FoxIDs.Infrastructure;
using FoxIDs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FoxIDs.Repository.MongoDb
{
    public class MongoDbTenantDataRepository : TenantDataRepositoryBase
    {
        private readonly MongoDbRepositoryClient mongoDbRepositoryClient;

        public MongoDbTenantDataRepository(MongoDbRepositoryClient mongoDbRepositoryClient)
        {
            this.mongoDbRepositoryClient = mongoDbRepositoryClient;
        }

        public override ValueTask<int> CountAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, bool usePartitionId = true)
        {
            throw new NotImplementedException();
        }

        public override ValueTask CreateAsync<T>(T item, TelemetryScopedLogger scopedLogger = null)
        {
            throw new NotImplementedException();
        }

        public override ValueTask<T> DeleteAsync<T>(string id, TelemetryScopedLogger scopedLogger = null)
        {
            throw new NotImplementedException();
        }

        public override ValueTask<int> DeleteListAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null, TelemetryScopedLogger scopedLogger = null)
        {
            throw new NotImplementedException();
        }

        public override ValueTask<bool> ExistsAsync<T>(string id, TelemetryScopedLogger scopedLogger = null)
        {
            throw new NotImplementedException();
        }

        public override ValueTask<T> GetAsync<T>(string id, bool required = true, bool delete = false, TelemetryScopedLogger scopedLogger = null)
        {
            var col = mongoDbRepositoryClient.GetCollection<T>();
            throw new NotImplementedException();
        }

        public override ValueTask<(HashSet<T> items, string continuationToken)> GetListAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, int maxItemCount = 50, string continuationToken = null, TelemetryScopedLogger scopedLogger = null)
        {
            throw new NotImplementedException();
        }

        public override ValueTask<Tenant> GetTenantByNameAsync(string tenantName, bool required = true, TelemetryScopedLogger scopedLogger = null)
        {
            throw new NotImplementedException();
        }

        public override ValueTask<Track> GetTrackByNameAsync(Track.IdKey idKey, bool required = true, TelemetryScopedLogger scopedLogger = null)
        {
            throw new NotImplementedException();
        }

        public override ValueTask SaveAsync<T>(T item, TelemetryScopedLogger scopedLogger = null)
        {
            throw new NotImplementedException();
        }

        public override ValueTask UpdateAsync<T>(T item, TelemetryScopedLogger scopedLogger = null)
        {
            throw new NotImplementedException();
        }
    }
}
