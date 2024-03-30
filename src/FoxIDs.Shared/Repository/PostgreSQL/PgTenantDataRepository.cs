using FoxIDs.Infrastructure;
using FoxIDs.Models;
using ITfoxtec.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Wololo.PgKeyValueDB;

namespace FoxIDs.Repository
{
    public class PgTenantDataRepository([FromKeyedServices("tenant")] PgKeyValueDB db) : TenantDataRepositoryBase
    {
        public override async ValueTask<bool> ExistsAsync<T>(string id, TelemetryScopedLogger scopedLogger = null)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = id.IdToTenantPartitionId();
            return await db.ExistsAsync(id, partitionId);
        }

        public override async ValueTask<int> CountAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, bool usePartitionId = true)  
        {
            var partitionId = usePartitionId ? PartitionIdFormat<T>(idKey) : null;
            if (whereQuery == null)
            {
                return (int) await db.CountAsync(partitionId);
            }
            else
            {
                var dataItems = await db.GetSetAsync<T>(partitionId);
                var lambda = whereQuery.Compile();
                return dataItems.Where(d => lambda(d)).Count();
            }
        }

        public override async ValueTask<T> GetAsync<T>(string id, bool required = true, bool delete = false, TelemetryScopedLogger scopedLogger = null)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = id.IdToTenantPartitionId();
            var item = await db.GetAsync<T>(id, partitionId);
            if (required && item == null)
                throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };
            return item;
        }

        public override async ValueTask<Tenant> GetTenantByNameAsync(string tenantName, bool required = true, TelemetryScopedLogger scopedLogger = null)
        {
            if (tenantName.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(tenantName));

            var id = await Tenant.IdFormatAsync(tenantName);
            var partitionId = Tenant.PartitionIdFormat();
            var item = await db.GetAsync<Tenant>(id, partitionId);
            if (required && item == null)
                throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };
            return item;
        }

        public override async ValueTask<Track> GetTrackByNameAsync(Track.IdKey idKey, bool required = true, TelemetryScopedLogger scopedLogger = null)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            var id = await Track.IdFormatAsync(idKey);
            var partitionId = Track.PartitionIdFormat(idKey);
            var item = await db.GetAsync<Track>(id, partitionId);
            if (required && item == null)
                throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };
            return item;
        }

        public override async ValueTask<(HashSet<T> items, string continuationToken)> GetListAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, int maxItemCount = 50, string continuationToken = null, TelemetryScopedLogger scopedLogger = null)
        {
            var partitionId = PartitionIdFormat<T>(idKey);
            var dataItems = await db.GetSetAsync<T>(partitionId, maxItemCount);
            continuationToken = null;
            if (whereQuery == null)
            {
                return (dataItems, continuationToken);
            }
            else
            {
                var lambda = whereQuery.Compile();
                return (dataItems.Where(d => lambda(d)).ToHashSet(), continuationToken);
            }
            throw new NotImplementedException();
        }

        public override async ValueTask CreateAsync<T>(T item, TelemetryScopedLogger scopedLogger = null)
        {
            await UpdateAsync(item);
        }

        public override async ValueTask UpdateAsync<T>(T item, TelemetryScopedLogger scopedLogger = null)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.SetTenantPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            await db.SetAsync(item.Id, item, item.PartitionId);
        }

        public override async ValueTask SaveAsync<T>(T item, TelemetryScopedLogger scopedLogger = null)
        {
            await UpdateAsync(item);
        }

        public override async ValueTask<T> DeleteAsync<T>(string id, TelemetryScopedLogger scopedLogger = null)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));
            
            var partitionId = id.IdToTenantPartitionId();
            var item = await db.GetAsync<T>(id, partitionId);
            await db.RemoveAsync(item.Id, partitionId);
            return item;
        }

        //public override ValueTask<T> DeleteAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null, TelemetryScopedLogger scopedLogger = null)
        //{
        //    throw new NotImplementedException();
        //}

        public override async ValueTask<int> DeleteListAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null, TelemetryScopedLogger scopedLogger = null)
        {
            /*if (idKey == null) new ArgumentNullException(nameof(idKey));

            await idKey.ValidateObjectAsync();
            var partitionId = PartitionIdFormat<T>(idKey);

            if (whereQuery == null)
            {
                return await fileDataRepository.DeleteListAsync(partitionId, GetDataType<T>());
            }
            else
            {
                var dataItems = (await fileDataRepository.GetListAsync(partitionId, GetDataType<T>())).Select(i => i.DataJsonToObject<T>());
                var lambda = whereQuery.Compile();
                var deleteItems = dataItems.Where(d => lambda(d));
                foreach (var item in deleteItems)
                {
                    _ = await fileDataRepository.DeleteAsync(item.Id, item.PartitionId);
                }
                return deleteItems.Count();
            }*/
            throw new NotImplementedException();
        }
    }
}
