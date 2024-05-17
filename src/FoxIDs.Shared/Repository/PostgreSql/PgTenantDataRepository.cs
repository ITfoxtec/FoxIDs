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
    public class PgTenantDataRepository([FromKeyedServices(Constants.Models.DataType.Tenant)] PgKeyValueDB db) : TenantDataRepositoryBase
    {
        public override async ValueTask<bool> ExistsAsync<T>(string id, TelemetryScopedLogger scopedLogger = null)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = id.IdToTenantPartitionId();
            return await db.ExistsAsync(id, partitionId);
        }

        public override async ValueTask<long> CountAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, bool usePartitionId = true)  
        {
            var partitionId = usePartitionId ? PartitionIdFormat<T>(idKey) : null;
            if (whereQuery == null)
            {
                return (int) await db.CountAsync(partitionId);
            }
            else
            {
                var dataItems = await db.GetHashSetAsync<T>(partitionId);
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
            { 
                throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };
            }
            if (item != null)
            {
                await item.ValidateObjectAsync();
            }
            return item;
        }

        public override async ValueTask<Tenant> GetTenantByNameAsync(string tenantName, bool required = true, TelemetryScopedLogger scopedLogger = null)
        {
            if (tenantName.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(tenantName));

            var id = await Tenant.IdFormatAsync(tenantName);
            var partitionId = Tenant.PartitionIdFormat();
            var item = await db.GetAsync<Tenant>(id, partitionId);
            if (required && item == null)
            {
                throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };
            }
            if (item != null)
            {
                await item.ValidateObjectAsync();
            }
            return item;
        }

        public override async ValueTask<Track> GetTrackByNameAsync(Track.IdKey idKey, bool required = true, TelemetryScopedLogger scopedLogger = null)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            var id = await Track.IdFormatAsync(idKey);
            var partitionId = Track.PartitionIdFormat(idKey);
            var item = await db.GetAsync<Track>(id, partitionId);
            if (required && item == null)
            {
                throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };
            }
            if (item != null)
            {
                await item.ValidateObjectAsync();
            }
            return item;
        }

        public override async ValueTask<(IReadOnlyCollection<T> items, string paginationToken)> GetListAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, int pageSize = Constants.Models.ListPageSize, string paginationToken = null, TelemetryScopedLogger scopedLogger = null)
        {
            //TODO pagination
            var partitionId = PartitionIdFormat<T>(idKey);
            var dataItems = await db.GetHashSetAsync<T>(partitionId, pageSize);
            paginationToken = null;
            if (whereQuery == null)
            {
                var items = dataItems;
                await items.ValidateObjectAsync();
                return (items, paginationToken);
            }
            else
            {
                var lambda = whereQuery.Compile();
                var items = dataItems.Where(d => lambda(d)).ToHashSet();
                await items.ValidateObjectAsync();
                return (items, paginationToken);
            }
            throw new NotImplementedException();
        }

        public override async ValueTask CreateAsync<T>(T item, TelemetryScopedLogger scopedLogger = null)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.SetTenantPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            if (!await db.CreateAsync(item.Id, item, item.PartitionId, expires: item is IDataTtlDocument ttlItem ? ttlItem.ExpireAt : null))
            {
                throw new FoxIDsDataException(item.Id, item.PartitionId) { StatusCode = DataStatusCode.Conflict };
            }
        }

        public override async ValueTask UpdateAsync<T>(T item, TelemetryScopedLogger scopedLogger = null)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.SetTenantPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            if(!await db.UpdateAsync(item.Id, item, item.PartitionId, expires: item is IDataTtlDocument ttlItem ? ttlItem.ExpireAt : null))
            {
                throw new FoxIDsDataException(item.Id, item.PartitionId) { StatusCode = DataStatusCode.NotFound };
            }
        }

        public override async ValueTask SaveAsync<T>(T item, TelemetryScopedLogger scopedLogger = null)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.SetTenantPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            await db.UpsertAsync(item.Id, item, item.PartitionId, expires: item is IDataTtlDocument ttlItem ? ttlItem.ExpireAt : null);
        }

        public override async ValueTask DeleteAsync<T>(string id, TelemetryScopedLogger scopedLogger = null)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));
            
            var partitionId = id.IdToTenantPartitionId();
            if(!await db.RemoveAsync(id, partitionId))
            {
                throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };

            }
        }

        //public override ValueTask DeleteAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null, TelemetryScopedLogger scopedLogger = null)
        //{
        //    throw new NotImplementedException();
        //}

        public override async ValueTask<long> DeleteListAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null, TelemetryScopedLogger scopedLogger = null)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            await idKey.ValidateObjectAsync();
            var partitionId = PartitionIdFormat<T>(idKey);

            if (whereQuery == null)
            {
                return await db.RemoveAllAsync(partitionId);
            }
            else
            {
                var dataItems = await db.GetHashSetAsync<T>(partitionId);
                var lambda = whereQuery.Compile();
                var deleteItems = dataItems.Where(d => lambda(d));
                foreach (var item in deleteItems)
                {
                    _ = await db.RemoveAsync(item.Id, item.PartitionId);
                }
                return deleteItems.Count();
            }
        }

        public async Task RemoveAllExpiredAsync()
        {
            _ = await db.RemoveAllExpiredAsync();
        }
    }
}
