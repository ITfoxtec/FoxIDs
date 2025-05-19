using FoxIDs.Infrastructure;
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
    public class MongoDbTenantDataRepository : TenantDataRepositoryBase
    {
        private readonly MongoDbRepositoryClient mongoDbRepositoryClient;

        public MongoDbTenantDataRepository(MongoDbRepositoryClient mongoDbRepositoryClient)
        {
            this.mongoDbRepositoryClient = mongoDbRepositoryClient;
        }

        public override async ValueTask<bool> ExistsAsync<T>(string id, bool queryAdditionalIds = false, TelemetryScopedLogger scopedLogger = null)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var item = await ReadItemAsync<T>(id, id.IdToTenantPartitionId(), false, queryAdditionalIds: queryAdditionalIds);
            return item != null;
        }

        public override async ValueTask<long> CountAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, bool usePartitionId = true)
        {
            var partitionId = usePartitionId ? PartitionIdFormat<T>(idKey) : null;

            Expression<Func<T, bool>> filter = usePartitionId ? f => f.PartitionId.Equals(partitionId) : f => true;
            filter = whereQuery == null ? filter : filter.AndAlso(whereQuery);
            try
            {
                var collection = mongoDbRepositoryClient.GetTenantsCollection<T>();
                return await collection.CountDocumentsAsync(filter);
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(partitionId, ex);
            }
        }

        public override async ValueTask<T> GetAsync<T>(string id, bool required = true, bool delete = false, bool queryAdditionalIds = false, TelemetryScopedLogger scopedLogger = null)
        {
            return await ReadItemAsync<T>(id, id.IdToTenantPartitionId(), required, delete: delete, queryAdditionalIds: queryAdditionalIds);
        }

        public override async ValueTask<Tenant> GetTenantByNameAsync(string tenantName, bool required = true, TelemetryScopedLogger scopedLogger = null)
        {
            if (tenantName.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(tenantName));

            return await ReadItemAsync<Tenant>(await Tenant.IdFormatAsync(tenantName), Tenant.PartitionIdFormat(), required);
        }

        public override async ValueTask<Track> GetTrackByNameAsync(Track.IdKey idKey, bool required = true, TelemetryScopedLogger scopedLogger = null)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            return await ReadItemAsync<Track>(await Track.IdFormatAsync(idKey), Track.PartitionIdFormat(idKey), required);
        }

        private async ValueTask<T> ReadItemAsync<T>(string id, string partitionId, bool required, bool delete = false, bool queryAdditionalIds = false) where T : IDataDocument
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));
            if (partitionId.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(partitionId));

            try
            {
                var collection = mongoDbRepositoryClient.GetTenantsCollection<T>();
                Expression<Func<T, bool>> filter = f => f.PartitionId.Equals(partitionId) && (f.Id.Equals(id) || (queryAdditionalIds && f.AdditionalIds.Where(a => a.Equals(id)).Any()));
                var data = delete ? await collection.FindOneAndDeleteAsync(filter) : await collection.Find(filter).FirstOrDefaultAsync();
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
            catch(FoxIDsDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(id, partitionId, ex);
            }
        }

        public override async ValueTask<(IReadOnlyCollection<T> items, string paginationToken)> GetListAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, int pageSize = Constants.Models.ListPageSize, string paginationToken = null, TelemetryScopedLogger scopedLogger = null)
        {
            var partitionId = PartitionIdFormat<T>(idKey);
            Expression<Func<T, bool>> filter = f => f.PartitionId.Equals(partitionId);
            filter = whereQuery == null ? filter : filter.AndAlso(whereQuery);

            try
            {
                var collection = mongoDbRepositoryClient.GetTenantsCollection<T>();
                var pageNumber = GetPageNumber(paginationToken);
                var items = pageNumber > 1 ? await collection.Find(filter).Skip((pageNumber - 1) * pageSize).Limit(pageSize + 1).ToListAsync() : await collection.Find(filter).Limit(pageSize + 1).ToListAsync();

                if(items.Count() > pageSize)
                {
                    items.RemoveAt(pageSize);
                    paginationToken = Convert.ToString(++pageNumber);
                }
                else
                {
                    paginationToken = null;
                }

                await items.ValidateObjectAsync();
                return (items, paginationToken);                
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(partitionId, ex);
            }
        }

        private int GetPageNumber(string paginationToken)
        {
            if (!paginationToken.IsNullOrEmpty() && int.TryParse(paginationToken, out int pageNumber))
            {
                return pageNumber;
            }
            return 1;
        }

        public override async ValueTask CreateAsync<T>(T item, TelemetryScopedLogger scopedLogger = null)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.SetTenantPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            try
            {
                var collection = mongoDbRepositoryClient.GetTenantsCollection(item);
                await collection.InsertOneAsync(item);
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(item.Id, item.PartitionId, ex);
            }
        }

        public override async ValueTask UpdateAsync<T>(T item, TelemetryScopedLogger scopedLogger = null)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.SetTenantPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            try
            {
                var collection = mongoDbRepositoryClient.GetTenantsCollection(item);
                var result = await collection.ReplaceOneAsync(f => f.PartitionId.Equals(item.PartitionId) && f.Id.Equals(item.Id), item);
                if (!result.IsAcknowledged || !(result.MatchedCount > 0))
                {
                    throw new FoxIDsDataException(item.Id, item.PartitionId) { StatusCode = DataStatusCode.NotFound };
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

        public override async ValueTask SaveAsync<T>(T item, TelemetryScopedLogger scopedLogger = null)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.SetTenantPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            try
            {
                var collection = mongoDbRepositoryClient.GetTenantsCollection(item);
                Expression<Func<T, bool>> filter = f => f.PartitionId.Equals(item.PartitionId) && f.Id.Equals(item.Id);
                await collection.ReplaceOneAsync(filter, item, options: new ReplaceOptions { IsUpsert = true });
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

        public override async ValueTask DeleteAsync<T>(string id, bool queryAdditionalIds = false, TelemetryScopedLogger scopedLogger = null)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = id.IdToTenantPartitionId();

            try
            {
                var collection = mongoDbRepositoryClient.GetTenantsCollection<T>();
                var result = await collection.DeleteOneAsync(f => f.PartitionId.Equals(partitionId) && (f.Id.Equals(id) || (queryAdditionalIds && f.AdditionalIds.Where(a => a.Equals(id)).Any())));
                if (!result.IsAcknowledged || !(result.DeletedCount > 0))
                {
                    throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };
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

        public override async ValueTask<long> DeleteListAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null, TelemetryScopedLogger scopedLogger = null)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            var partitionId = PartitionIdFormat<T>(idKey);
            Expression<Func<T, bool>> filter = f => f.PartitionId.Equals(partitionId);
            filter = whereQuery == null ? filter : filter.AndAlso(whereQuery);

            try
            {
                var collection = mongoDbRepositoryClient.GetTenantsCollection<T>();
                var result = await collection.DeleteManyAsync(filter);
                return result.DeletedCount;
            }
            catch (Exception ex)
            {
                throw new FoxIDsDataException(partitionId, ex);
            }
        }
    }
}
