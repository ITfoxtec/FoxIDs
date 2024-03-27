using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Repository;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FoxIDs.UnitTests.Mocks
{
    public class FakeTenantRepository : TenantDataRepositoryBase
    {

        public override ValueTask<bool> ExistsAsync<T>(string id, TelemetryScopedLogger scopedLogger = null)
        {
            if(id == "user:testtenant:testtrack:a2@test.com")
            {
                return ValueTask.FromResult(true);
            }

            return ValueTask.FromResult(false);
        }

        public override ValueTask<int> CountAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, bool usePartitionId = true)
        {
            throw new NotImplementedException();
        }

        public override ValueTask<T> GetAsync<T>(string id, bool required = true, bool delete = false, TelemetryScopedLogger scopedLogger = null)
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

        public override ValueTask<(HashSet<T> items, string continuationToken)> GetListAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, int maxItemCount = 50, string continuationToken = null, TelemetryScopedLogger scopedLogger = null)
        {
            throw new NotImplementedException();
        }

        public override ValueTask CreateAsync<T>(T item, TelemetryScopedLogger scopedLogger = null)
        {
            return ValueTask.CompletedTask;
        }

        public override ValueTask UpdateAsync<T>(T item, TelemetryScopedLogger scopedLogger = null)
        {
            throw new NotImplementedException();
        }

        public override ValueTask SaveAsync<T>(T item, TelemetryScopedLogger scopedLogger = null)
        {
            throw new NotImplementedException();
        }

        public override ValueTask<T> DeleteAsync<T>(string id, TelemetryScopedLogger scopedLogger = null)
        {
            throw new NotImplementedException();
        }

        //public override ValueTask<T> DeleteAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null, TelemetryScopedLogger scopedLogger = null)
        //{
        //    throw new NotImplementedException();
        //}

        public override ValueTask<int> DeleteListAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null, TelemetryScopedLogger scopedLogger = null)
        {
            throw new NotImplementedException();
        }
    }
}
