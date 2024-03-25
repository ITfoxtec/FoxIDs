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
        public ValueTask<bool> ExistsAsync<T>(string id, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument
        {
            throw new NotImplementedException();
        }

        public ValueTask<int> CountAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, bool usePartitionId = true) where T : IDataDocument
        {
            throw new NotImplementedException();
        }

        public ValueTask<T> GetAsync<T>(string id, bool required = true, bool delete = false, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument
        {
            throw new NotImplementedException();
        }

        public ValueTask<Tenant> GetTenantByNameAsync(string tenantName, bool required = true, TelemetryScopedLogger scopedLogger = null)
        {
            throw new NotImplementedException();
        }

        public ValueTask<Track> GetTrackByNameAsync(Track.IdKey idKey, bool required = true, TelemetryScopedLogger scopedLogger = null)
        {
            throw new NotImplementedException();
        }

        public ValueTask<(HashSet<T> items, string continuationToken)> GetListAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, int maxItemCount = 50, string continuationToken = null, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument
        {
            throw new NotImplementedException();
        }

        public ValueTask CreateAsync<T>(T item, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument
        {
            throw new NotImplementedException();
        }

        public ValueTask UpdateAsync<T>(T item, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument
        {
            throw new NotImplementedException();
        }

        public ValueTask SaveAsync<T>(T item, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument
        {
            throw new NotImplementedException();
        }

        public ValueTask<T> DeleteAsync<T>(string id, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument
        {
            throw new NotImplementedException();
        }

        //public ValueTask<T> DeleteAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument
        //{
        //    throw new NotImplementedException();
        //}

        public ValueTask<int> DeleteListAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument
        {
            throw new NotImplementedException();
        }
    }
}
