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
        public override async ValueTask<bool> ExistsAsync<T>(string id, bool queryAdditionalIds = false, TelemetryScopedLogger scopedLogger = null)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = id.IdToTenantPartitionId();
            if (!queryAdditionalIds)
            {
                return await db.ExistsAsync(id, partitionId);
            }
            else
            {
                var item = await GetAsync<T>(id, required: false, queryAdditionalIds: queryAdditionalIds, scopedLogger: scopedLogger);
                return item != null;
            }
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
                if (item != null && delete)
                {
                    if (!await db.RemoveAsync(id, partitionId))
                    {
                        throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };
                    }
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
                if (item != null && delete)
                {
                    if (!await db.RemoveAsync(item.Id, partitionId))
                    {
                        throw new FoxIDsDataException(item.Id, partitionId) { StatusCode = DataStatusCode.NotFound };
                    }
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

            if (item.AdditionalIds?.Count() > 0)
            {
                foreach (var additionalId in item.AdditionalIds)
                {
                    if (await AdditionalIdExistAsync<T>(additionalId, item.PartitionId, scopedLogger: scopedLogger))
                    {
                        throw new FoxIDsDataException(additionalId, item.PartitionId) { StatusCode = DataStatusCode.Conflict };
                    }
                }
            }

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

            if (item.AdditionalIds?.Count() > 0)
            {
                foreach (var additionalId in item.AdditionalIds)
                {
                    if (await AdditionalIdExistAsync<T>(additionalId, item.PartitionId, notId: item.Id, scopedLogger: scopedLogger))
                    {
                        throw new FoxIDsDataException(additionalId, item.PartitionId) { StatusCode = DataStatusCode.Conflict };
                    }
                }
            }

            if (!await db.UpdateAsync(item.Id, item, item.PartitionId, expires: item is IDataTtlDocument ttlItem ? ttlItem.ExpireAt : null))
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

            if (item.AdditionalIds?.Count() > 0)
            {
                var exist = await ExistsAsync<T>(item.Id, scopedLogger: scopedLogger);
                foreach (var additionalId in item.AdditionalIds)
                {
                    if (await AdditionalIdExistAsync<T>(additionalId, item.PartitionId, notId: exist ? item.Id : null, scopedLogger: scopedLogger))
                    {
                        throw new FoxIDsDataException(additionalId, item.PartitionId) { StatusCode = DataStatusCode.Conflict };
                    }
                }
            }

            await db.UpsertAsync(item.Id, item, item.PartitionId, expires: item is IDataTtlDocument ttlItem ? ttlItem.ExpireAt : null);
        }

        private async Task<bool> AdditionalIdExistAsync<T>(string idOrAdditionalId, string partitionId, string notId = null, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument
        {
            var notIdIsEmpty = notId.IsNullOrEmpty();
            Expression<Func<T, bool>> whereQuery = q => (notIdIsEmpty || q.Id != notId) && (q.Id == idOrAdditionalId || q.AdditionalIds.Contains(idOrAdditionalId));
            var item = await db.GetListAsync(partitionId, whereQuery, 1).FirstOrDefaultAsync();
            return item != null;
        }

        public override async ValueTask SaveListAsync<T>(IReadOnlyCollection<T> items, TelemetryScopedLogger scopedLogger = null)
        {
            if (items?.Count <= 0) new ArgumentNullException(nameof(items));
            var firstItem = items.First();
            if (firstItem.Id.IsNullOrEmpty()) throw new ArgumentNullException($"First item {nameof(firstItem.Id)}.", items.GetType().Name);

            var partitionId = firstItem.Id.IdToTenantPartitionId();
            foreach (var item in items)
            {
                item.PartitionId = partitionId;
                item.SetDataType();
                await item.ValidateObjectAsync();
            }

            foreach (var item in items)
            {
                await SaveAsync(item, scopedLogger: scopedLogger);
            }
        }

        public override async ValueTask DeleteAsync<T>(string id, bool queryAdditionalIds = false, TelemetryScopedLogger scopedLogger = null)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));
            
            if(!queryAdditionalIds)
            {
                var partitionId = id.IdToTenantPartitionId();
                if (!await db.RemoveAsync(id, partitionId))
                {
                    throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };
                }
            }
            else
            {
                _ = await GetAsync<T>(id, required: true, delete: true, queryAdditionalIds: queryAdditionalIds, scopedLogger: scopedLogger);
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

        public override async ValueTask DeleteListAsync<T>(IReadOnlyCollection<string> ids, bool queryAdditionalIds = false, TelemetryScopedLogger scopedLogger = null)
        {
            foreach (string id in ids)
            {
                if (!queryAdditionalIds)
                {
                    var partitionId = id.IdToTenantPartitionId();
                    _ = await db.RemoveAsync(id, partitionId);
                }
                else
                {
                    _ = await GetAsync<T>(id, required: false, delete: true, queryAdditionalIds: queryAdditionalIds, scopedLogger: scopedLogger);
                }
            }
        }

        public async Task RemoveAllExpiredGlobalAsync()
        {
            _ = await db.RemoveAllExpiredGlobalAsync();
        }
    }
}
