using FoxIDs.Models;
using ITfoxtec.Identity;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FoxIDs.Repository
{
    public class MongoDbMasterDataRepository : MasterDataRepositoryBase
    {
        private readonly MongoDbRepositoryClient mongoDbRepositoryClient;

        public MongoDbMasterDataRepository(MongoDbRepositoryClient mongoDbRepositoryClient)
        {
            this.mongoDbRepositoryClient = mongoDbRepositoryClient;
        }

        public override async ValueTask<bool> ExistsAsync<T>(string id)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var item = await ReadItemAsync<T>(id, id.IdToTenantPartitionId(), false);
            return item != null;
        }

        public override async ValueTask<long> CountAsync<T>(Expression<Func<T, bool>> whereQuery = null)
        {
            var partitionId = IdToMasterPartitionId<T>();
            Expression<Func<T, bool>> filter = f => f.PartitionId.Equals(partitionId, StringComparison.Ordinal);
            filter = whereQuery == null ? filter : Expression.Lambda<Func<T, bool>>(Expression.AndAlso(filter, whereQuery));

            try
            {
                var collection = mongoDbRepositoryClient.GetTenantsCollection<T>();
                return await collection.CountDocumentsAsync(whereQuery);
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(partitionId, ex);
            }
        }

        public override async ValueTask<T> GetAsync<T>(string id, bool required = true)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            return await ReadItemAsync<T>(id, id.IdToMasterPartitionId(), required);
        }

        private async ValueTask<T> ReadItemAsync<T>(string id, string partitionId, bool required) where T : IDataElement
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));
            if (partitionId.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(partitionId));

            try
            {
                var collection = mongoDbRepositoryClient.GetTenantsCollection<T>();
                Expression<Func<T, bool>> filter = d => d.Id.Equals(id, StringComparison.Ordinal);
                var data = await collection.Find(filter).FirstOrDefaultAsync();
                if (required && data == null)
                {
                    throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };
                }
                return data;
            }
            catch (FoxIDsDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(id, partitionId, ex);
            }
        }

        public override async ValueTask<List<T>> GetListAsync<T>(Expression<Func<T, bool>> whereQuery = null, int maxItemCount = 50)
        {
            var partitionId = IdToMasterPartitionId<T>();
            Expression<Func<T, bool>> filter = f => f.PartitionId.Equals(partitionId, StringComparison.Ordinal);
            filter = whereQuery == null ? filter : Expression.Lambda<Func<T, bool>>(Expression.AndAlso(filter, whereQuery));

            try
            {
                var collection = mongoDbRepositoryClient.GetTenantsCollection<T>();
                var items = await collection.Find(filter).Limit(maxItemCount).ToListAsync();
                return items;
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(ex);
            }
        }

        public override async ValueTask CreateAsync<T>(T item)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.PartitionId = item.Id.IdToMasterPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            try
            {
                var collection = mongoDbRepositoryClient.GetTenantsCollection<T>();
                await collection.InsertOneAsync(item);
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(item.Id, item.PartitionId, ex);
            }
        }

        public override async ValueTask UpdateAsync<T>(T item)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.PartitionId = item.Id.IdToMasterPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            try
            {
                var collection = mongoDbRepositoryClient.GetTenantsCollection<T>();
                await collection.ReplaceOneAsync(d => d.Id.Equals(item.Id, StringComparison.Ordinal), item);
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(item.Id, item.PartitionId, ex);
            }
        }

        public override async ValueTask SaveAsync<T>(T item)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.PartitionId = item.Id.IdToMasterPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            try
            {
                var collection = mongoDbRepositoryClient.GetTenantsCollection<T>();
                await collection.ReplaceOneAsync(d => d.Id.Equals(item.Id, StringComparison.Ordinal), item, new ReplaceOptions { IsUpsert = true });
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(item.Id, item.PartitionId, ex);
            }
        }

        public override async ValueTask DeleteAsync<T>(T item)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            var partitionId = item.Id.IdToMasterPartitionId();

            try
            {
                var collection = mongoDbRepositoryClient.GetTenantsCollection<T>();
                var result = await collection.DeleteOneAsync(d => d.Id.Equals(item.Id, StringComparison.Ordinal));
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(item.Id, item.PartitionId, ex);
            }
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

            try
            {
                var collection = mongoDbRepositoryClient.GetTenantsCollection<T>();
                await collection.InsertManyAsync(items);
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(partitionId, ex);
            }
        }

        public override async ValueTask DeleteAsync<T>(string id)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = id.IdToMasterPartitionId();

            try
            {
                var collection = mongoDbRepositoryClient.GetTenantsCollection<T>();
                var result = await collection.DeleteOneAsync(d => d.Id.Equals(id, StringComparison.Ordinal));
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(id, partitionId, ex);
            }
        }

        public override async ValueTask DeleteBulkAsync<T>(List<string> ids)
        {
            if (ids?.Count <= 0) new ArgumentNullException(nameof(ids));
            var firstId = ids.First();
            if (firstId.IsNullOrEmpty()) throw new ArgumentNullException($"First id {nameof(firstId)}.", ids.GetType().Name);

            var partitionId = firstId.IdToMasterPartitionId();

            try
            {
                var collection = mongoDbRepositoryClient.GetTenantsCollection<T>();
                var result = await collection.DeleteManyAsync(d => d.PartitionId.Equals(partitionId, StringComparison.Ordinal) && ids.Contains(d.Id, StringComparer.Ordinal));
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(partitionId, ex);
            }
        }
    }
}
