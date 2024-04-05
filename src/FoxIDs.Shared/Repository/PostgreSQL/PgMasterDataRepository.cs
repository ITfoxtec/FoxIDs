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
    public class PgMasterDataRepository([FromKeyedServices(Constants.Models.DataType.Master)] PgKeyValueDB db) : MasterDataRepositoryBase
    {
        public override async ValueTask<bool> ExistsAsync<T>(string id)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = id.IdToMasterPartitionId();
            return await db.ExistsAsync(id, partitionId);
        }

        public override async ValueTask<long> CountAsync<T>(Expression<Func<T, bool>> whereQuery = null)
        {
            var partitionId = IdToMasterPartitionId<T>();
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

        public override async ValueTask<T> GetAsync<T>(string id, bool required = true)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = id.IdToMasterPartitionId();
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

        public override async ValueTask<List<T>> GetListAsync<T>(Expression<Func<T, bool>> whereQuery = null, int maxItemCount = 50)
        {
            var partitionId = IdToMasterPartitionId<T>();
            var dataItems = await db.GetSetAsync<T>(partitionId, maxItemCount);
            if (whereQuery == null)
            {
                var items = dataItems.ToList();
                await items.ValidateObjectAsync();
                return items;
            }
            else
            {
                var lambda = whereQuery.Compile();
                var items = dataItems.Where(d => lambda(d)).ToList();
                await items.ValidateObjectAsync();
                return items;
            }
            throw new NotImplementedException();
        }

        public override async ValueTask CreateAsync<T>(T item)
        {
            //TODO fail if the ID already exists
            //  throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.Conflict };
            await UpdateAsync(item);
        }

        public override async ValueTask UpdateAsync<T>(T item)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.PartitionId = item.Id.IdToMasterPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            //TODO fail if the ID do not exists
            //  throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };
            await db.SetAsync(item.Id, item, item.PartitionId);
        }

        public override async ValueTask SaveAsync<T>(T item)
        {
            await UpdateAsync(item);
        }

        public override async ValueTask DeleteAsync<T>(T item)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.PartitionId = item.Id.IdToMasterPartitionId();
            await item.ValidateObjectAsync();

            await db.RemoveAsync(item.Id, item.PartitionId);
        }

        public override async ValueTask SaveBulkAsync<T>(List<T> items)
        {
            if (items?.Count <= 0) new ArgumentNullException(nameof(items));
            var firstItem = items.First();
            if (firstItem.Id.IsNullOrEmpty()) throw new ArgumentNullException($"First item {nameof(firstItem.Id)}.", items.GetType().Name);

            var partitionId = firstItem.Id.IdToMasterPartitionId();
            foreach (var item in items)
            {
                item.PartitionId = partitionId;
                item.SetDataType();
                await item.ValidateObjectAsync();
            }

            foreach (var item in items)
            {
                await SaveAsync(item);
            }
        }

        public override async ValueTask DeleteAsync<T>(string id)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = id.IdToTenantPartitionId();
            //TODO fail if the ID do not exists
            //  throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };
            await db.RemoveAsync(id, partitionId);
        }

        public override async ValueTask DeleteBulkAsync<T>(List<string> ids)
        {
            foreach (string id in ids)
            {
                var partitionId = id.IdToMasterPartitionId();
                _ = await db.RemoveAsync(id, partitionId);
            }
        }
    }
}
