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
            return (int) await db.CountAsync(partitionId, whereQuery);
        }

        public override async ValueTask<T> GetAsync<T>(string id, bool required = true, bool delete = false, bool queryAdditionalIds = false, TelemetryScopedLogger scopedLogger = null)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = id.IdToTenantPartitionId();
            if (!queryAdditionalIds)
            {
                var item = await db.GetAsync<T>(id, partitionId);
                if (required && item == null)
                {
                    throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };
                }
                await item.ValidateObjectAsync();
                return item;
            }
            else
            {
                Expression<Func<T, bool>> whereQuery = q => q.Id == id || q.AdditionalIds.Contains(id);
                var item = await db.GetListAsync(partitionId, whereQuery, 1).FirstOrDefaultAsync();
                if (required && item == null)
                {
                    throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };
                }
                await item.ValidateObjectAsync();
                return item;
            }
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
            await item.ValidateObjectAsync();
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
            await item.ValidateObjectAsync();
            return item;
        }

        public override async ValueTask<(IReadOnlyCollection<T> items, string paginationToken)> GetListAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, int pageSize = Constants.Models.ListPageSize, string paginationToken = null, TelemetryScopedLogger scopedLogger = null)
        {
            var offset = GetOffset(paginationToken, pageSize);
            var partitionId = PartitionIdFormat<T>(idKey);
            var dataItems = await db.GetListAsync(partitionId, whereQuery, pageSize + 1, offset).ToListAsync();
            paginationToken = NextPaginationToken(paginationToken, pageSize, dataItems.Count);
            var items = dataItems;
            await items.ValidateObjectAsync();
            return (items, paginationToken);
        }

        private static int GetOffset(string paginationToken, int pageSize)
        {
            if (!paginationToken.IsNullOrEmpty() && int.TryParse(paginationToken, out int pageNumber))
                return (pageNumber - 1) * pageSize;
            return 0;
        }

        private static string NextPaginationToken(string paginationToken, int pageSize, int itemCount)
        {
            if (itemCount < pageSize)
                return null;
            if (!paginationToken.IsNullOrEmpty() && int.TryParse(paginationToken, out int pageNumber))
                return (pageNumber + 1).ToString();
            return "1";
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
            return await db.RemoveAllAsync(partitionId, whereQuery);
        }

        public async Task RemoveAllExpiredAsync()
        {
            _ = await db.RemoveAllExpiredAsync();
        }
    }
}
