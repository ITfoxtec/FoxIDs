using FoxIDs.Infrastructure;
using FoxIDs.Logic;
using FoxIDs.Models;
using ITfoxtec.Identity;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
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
        private readonly AuditLogic auditLogic;

        public MongoDbMasterDataRepository(MongoDbRepositoryClient mongoDbRepositoryClient, AuditLogic auditLogic)
        {
            this.mongoDbRepositoryClient = mongoDbRepositoryClient;
            this.auditLogic = auditLogic;
        }

        public override async ValueTask<bool> ExistsAsync<T>(string id)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var item = await ReadItemAsync<T>(id, id.IdToMasterPartitionId(), false);
            return item != null;
        }

        public override async ValueTask<long> CountAsync<T>(Expression<Func<T, bool>> whereQuery = null)
        {
            var partitionId = TypeToMasterPartitionId<T>();
            Expression<Func<T, bool>> filter = f => f.PartitionId.Equals(partitionId);
            filter = whereQuery == null ? filter : filter.AndAlso(whereQuery);

            try
            {
                var collection = mongoDbRepositoryClient.GetMasterCollection<T>();
                return await collection.CountDocumentsAsync(filter);
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

        private async ValueTask<T> ReadItemAsync<T>(string id, string partitionId, bool required) where T : MasterDocument
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));
            if (partitionId.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(partitionId));

            try
            {
                var collection = mongoDbRepositoryClient.GetMasterCollection<T>();
                Expression<Func<T, bool>> filter = f => f.PartitionId.Equals(partitionId) && f.Id.Equals(id);
                var data = await collection.Find(filter).FirstOrDefaultAsync();
                if (required && data == null)
                {
                    throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };
                }
                if (data != null)
                {
                    await data.ValidateObjectAsync();
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

        public override async ValueTask<IReadOnlyCollection<T>> GetManyAsync<T>(Expression<Func<T, bool>> whereQuery = null, int pageSize = Constants.Models.ListPageSize)
        {
            var partitionId = TypeToMasterPartitionId<T>();
            Expression<Func<T, bool>> filter = f => f.PartitionId.Equals(partitionId);
            filter = whereQuery == null ? filter : filter.AndAlso(whereQuery);

            try
            {
                var collection = mongoDbRepositoryClient.GetMasterCollection<T>();
                var items = await collection.Find(filter).Limit(pageSize).ToListAsync();
                await items.ValidateObjectAsync();
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

            var shouldAudit = auditLogic.ShouldAudit();
            try
            {
                var collection = mongoDbRepositoryClient.GetMasterCollection(item);
                await collection.InsertOneAsync(item);

                if (shouldAudit)
                {
                    await auditLogic.LogAsync(AuditAction.Create, null, item, item.PartitionId, item.Id);
                }
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

            var shouldAudit = auditLogic.ShouldAudit();
            try
            {
                var collection = mongoDbRepositoryClient.GetMasterCollection(item);
                var filter = Builders<T>.Filter.Where(f => f.PartitionId.Equals(item.PartitionId) && f.Id.Equals(item.Id));
                var existing = await collection.Find(filter).FirstOrDefaultAsync();
                if (existing == null)
                {
                    throw new FoxIDsDataException(item.Id, item.PartitionId) { StatusCode = DataStatusCode.NotFound };
                }

                var result = await collection.ReplaceOneAsync(filter, item);
                if (!result.IsAcknowledged || !(result.MatchedCount > 0))
                {
                    throw new FoxIDsDataException(item.Id, item.PartitionId) { StatusCode = DataStatusCode.NotFound };
                }

                if (shouldAudit)
                {
                    await auditLogic.LogAsync(AuditAction.Update, existing, item, item.PartitionId, item.Id);
                }
            }
            catch (FoxIDsDataException)
            {
                throw;
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

            var shouldAudit = auditLogic.ShouldAudit();
            try
            {
                var collection = mongoDbRepositoryClient.GetMasterCollection(item);
                Expression<Func<T, bool>> filter = f => f.PartitionId.Equals(item.PartitionId) && f.Id.Equals(item.Id);
                T existing = default;
                if (shouldAudit)
                {
                    existing = await collection.Find(filter).FirstOrDefaultAsync();
                }
                await collection.ReplaceOneAsync(filter, item, options: new ReplaceOptions { IsUpsert = true });

                if (shouldAudit)
                {
                    await auditLogic.LogAsync(AuditAction.Save, existing, item, item.PartitionId, item.Id);
                }
            }
            catch (FoxIDsDataException)
            {
                throw;
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
            var shouldAudit = auditLogic.ShouldAudit();

            try
            {
                var collection = mongoDbRepositoryClient.GetMasterCollection(item);
                var result = await collection.DeleteOneAsync(f => f.PartitionId.Equals(item.PartitionId) && f.Id.Equals(item.Id));
                if (!result.IsAcknowledged || !(result.DeletedCount > 0))
                {
                    throw new FoxIDsDataException(item.Id, item.PartitionId) { StatusCode = DataStatusCode.NotFound };
                }

                if (shouldAudit)
                {
                    await auditLogic.LogAsync(AuditAction.Delete, item, null, partitionId, item.Id);
                }
            }
            catch (FoxIDsDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(item.Id, item.PartitionId, ex);
            }
        }

        public override async ValueTask SaveManyAsync<T>(IReadOnlyCollection<T> items)
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

            var shouldAudit = auditLogic.ShouldAudit();
            try
            {
                var collection = mongoDbRepositoryClient.GetMasterCollection(firstItem);
                Dictionary<string, T> existingLookup = null;
                if (shouldAudit)
                {
                    var ids = items.Select(i => i.Id).ToList();
                    var existingItems = await collection.Find(Builders<T>.Filter.In(d => d.Id, ids)).ToListAsync();
                    existingLookup = existingItems?.ToDictionary(e => e.Id);
                }

                var updates = new List<WriteModel<T>>();
                foreach (var item in items)
                {
                    updates.Add(new ReplaceOneModel<T>(Builders<T>.Filter.Where(d => d.Id == item.Id), item) { IsUpsert = true });
                }
                await collection.BulkWriteAsync(updates, new BulkWriteOptions() { IsOrdered = false });

                if (shouldAudit)
                {
                    foreach (var item in items)
                    {
                        T existing = default;
                        if (existingLookup != null && existingLookup.TryGetValue(item.Id, out var existingValue))
                        {
                            existing = existingValue;
                        }
                        await auditLogic.LogAsync(AuditAction.Save, existing, item, partitionId, item.Id);
                    }
                }
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
            var shouldAudit = auditLogic.ShouldAudit();

            try
            {
                var collection = mongoDbRepositoryClient.GetMasterCollection<T>();
                Expression<Func<T, bool>> filter = f => f.PartitionId.Equals(partitionId) && f.Id.Equals(id);
                var existing = await collection.FindOneAndDeleteAsync(filter);
                if (existing == null)
                {
                    throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };
                }

                if (shouldAudit)
                {
                    await auditLogic.LogAsync(AuditAction.Delete, existing, null, partitionId, existing.Id);
                }
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

        public override async ValueTask DeleteManyAsync<T>(IReadOnlyCollection<string> ids)
        {
            if (ids?.Count <= 0) new ArgumentNullException(nameof(ids));
            var firstId = ids.First();
            if (firstId.IsNullOrEmpty()) throw new ArgumentNullException($"First id {nameof(firstId)}.", ids.GetType().Name);

            var partitionId = firstId.IdToMasterPartitionId();
            var shouldAudit = auditLogic.ShouldAudit();

            try
            {
                var collection = mongoDbRepositoryClient.GetMasterCollection<T>();
                foreach (var id in ids)
                {
                    Expression<Func<T, bool>> filter = f => f.PartitionId.Equals(partitionId) && f.Id.Equals(id);
                    var existing = await collection.FindOneAndDeleteAsync(filter);

                    if (shouldAudit && existing != null)
                    {
                        await auditLogic.LogAsync(AuditAction.Delete, existing, null, partitionId, existing.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(partitionId, ex);
            }
        }

        public override async ValueTask DeleteManyAsync<T>()
        {
            var partitionId = TypeToMasterPartitionId<T>();
            var shouldAudit = auditLogic.ShouldAudit();
            try
            {
                var collection = mongoDbRepositoryClient.GetMasterCollection<T>();
                List<T> existingItems = null;
                if (shouldAudit)
                {
                    existingItems = await collection.Find(f => f.PartitionId.Equals(partitionId)).ToListAsync();
                }
                await collection.DeleteManyAsync(f => f.PartitionId.Equals(partitionId));

                if (shouldAudit && existingItems?.Count > 0)
                {
                    foreach (var existing in existingItems)
                    {
                        await auditLogic.LogAsync(AuditAction.Delete, existing, null, partitionId, existing.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(partitionId, ex);
            }
        }
    }
}
