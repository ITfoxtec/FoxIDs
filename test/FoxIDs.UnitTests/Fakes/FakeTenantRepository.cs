using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FoxIDs.UnitTests.Mocks
{
    public class FakeTenantRepository : ITenantDataRepository
    {

        public ValueTask<bool> ExistsAsync<T>(string id, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument
        {
            if(id == "user:testtenant:testtrack:a2@test.com")
            {
                return ValueTask.FromResult(true);
            }

            return ValueTask.FromResult(false);
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
            return ValueTask.CompletedTask;
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

        public ValueTask<T> DeleteAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument
        {
            throw new NotImplementedException();
        }

        public ValueTask<int> DeleteListAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument
        {
            throw new NotImplementedException();
        }
    }
}
