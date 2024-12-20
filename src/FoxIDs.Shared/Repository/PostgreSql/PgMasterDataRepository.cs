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
            var partitionId = TypeToMasterPartitionId<T>();
            return (int) await db.CountAsync(partitionId, whereQuery);
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
            await item.ValidateObjectAsync();
            return item;
        }

        public override async ValueTask<IReadOnlyCollection<T>> GetListAsync<T>(Expression<Func<T, bool>> whereQuery = null, int pageSize = Constants.Models.ListPageSize)
        {
            var partitionId = TypeToMasterPartitionId<T>();
            var dataItems = await db.GetListAsync(partitionId, whereQuery, pageSize).ToListAsync();
            var items = dataItems.ToList();
            await items.ValidateObjectAsync();
            return items;
        }

        public override async ValueTask CreateAsync<T>(T item)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.PartitionId = item.Id.IdToMasterPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            if (!await db.CreateAsync(item.Id, item, item.PartitionId))
            {
                throw new FoxIDsDataException(item.Id, item.PartitionId) { StatusCode = DataStatusCode.Conflict };
            }
        }

        public override async ValueTask UpdateAsync<T>(T item)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.PartitionId = item.Id.IdToMasterPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            if(!await db.UpdateAsync(item.Id, item, item.PartitionId))
            {
                throw new FoxIDsDataException(item.Id, item.PartitionId) { StatusCode = DataStatusCode.NotFound };
            }
        }

        public override async ValueTask SaveAsync<T>(T item)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.PartitionId = item.Id.IdToMasterPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            await db.UpsertAsync(item.Id, item, item.PartitionId);
        }

        public override async ValueTask DeleteAsync<T>(T item)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.PartitionId = item.Id.IdToMasterPartitionId();
            await item.ValidateObjectAsync();

            await db.RemoveAsync(item.Id, item.PartitionId);
        }

        public override async ValueTask SaveBulkAsync<T>(IReadOnlyCollection<T> items)
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

            var partitionId = id.IdToMasterPartitionId();

            if(!await db.RemoveAsync(id, partitionId))
            {
                throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };
            }
        }

        public override ValueTask DeleteBulkAsync<T>()
        {
            throw new NotSupportedException("Not supported by PostgreSql.");
        }

        public override async ValueTask DeleteBulkAsync<T>(IReadOnlyCollection<string> ids)
        {
            foreach (string id in ids)
            {
                var partitionId = id.IdToMasterPartitionId();
                _ = await db.RemoveAsync(id, partitionId);
            }
        }
    }
}
