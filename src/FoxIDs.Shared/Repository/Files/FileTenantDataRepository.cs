using FoxIDs.Infrastructure;
using FoxIDs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FoxIDs.Repository
{
    public class FileTenantDataRepository : ITenantDataRepository
    {
        public Task<bool> ExistsAsync<T>(string id, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument
        {
            throw new NotImplementedException();
        }

        public Task<int> CountAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, bool usePartitionId = true) where T : IDataDocument
        {
            throw new NotImplementedException();
        }

        public Task<T> GetAsync<T>(string id, bool required = true, bool delete = false, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument
        {
            throw new NotImplementedException();
        }

        public Task<Tenant> GetTenantByNameAsync(string tenantName, bool required = true, TelemetryScopedLogger scopedLogger = null)
        {
            throw new NotImplementedException();
        }

        public Task<Track> GetTrackByNameAsync(Track.IdKey idKey, bool required = true, TelemetryScopedLogger scopedLogger = null)
        {
            throw new NotImplementedException();
        }

        public Task<(HashSet<T> items, string continuationToken)> GetListAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, int maxItemCount = 50, string continuationToken = null, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument
        {
            throw new NotImplementedException();
        }

        public Task CreateAsync<T>(T item, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync<T>(T item, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument
        {
            throw new NotImplementedException();
        }

        public Task SaveAsync<T>(T item, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument
        {
            throw new NotImplementedException();
        }

        public Task<T> DeleteAsync<T>(string id, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument
        {
            throw new NotImplementedException();
        }

        public Task<T> DeleteAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument
        {
            throw new NotImplementedException();
        }

        public Task<int> DeleteListAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument
        {
            throw new NotImplementedException();
        }
    }
}
