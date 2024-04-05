using FoxIDs.Infrastructure;
using FoxIDs.Models;
using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FoxIDs.Repository
{
    public class MemoryTenantDataRepository : TenantDataRepositoryBase
    {
        private readonly MemoryDataRepository memoryDataRepository;

        public MemoryTenantDataRepository(MemoryDataRepository memoryDataRepository)
        {
            this.memoryDataRepository = memoryDataRepository;
        }

        public override ValueTask<bool> ExistsAsync<T>(string id, TelemetryScopedLogger scopedLogger = null)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = id.IdToTenantPartitionId();
            return memoryDataRepository.ExistsAsync(id, partitionId);
        }

        public override async ValueTask<long> CountAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, bool usePartitionId = true)
        {
            var partitionId = usePartitionId ? PartitionIdFormat<T>(idKey) : null;

            if (whereQuery == null)
            {
                return await memoryDataRepository.CountAsync(partitionId);
            }
            else
            {
                var dataItems = (await memoryDataRepository.GetListAsync(partitionId)).Select(i => i.DataJsonToObject<T>());
                var lambda = whereQuery.Compile();
                return dataItems.Where(d => lambda(d)).Count();
            }
        }

        public override async ValueTask<T> GetAsync<T>(string id, bool required = true, bool delete = false, TelemetryScopedLogger scopedLogger = null)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = id.IdToTenantPartitionId();
            return (await memoryDataRepository.GetAsync(id, partitionId, required, delete)).DataJsonToObject<T>();
        }

        public override async ValueTask<Tenant> GetTenantByNameAsync(string tenantName, bool required = true, TelemetryScopedLogger scopedLogger = null)
        {
            if (tenantName.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(tenantName));

            var id = await Tenant.IdFormatAsync(tenantName);
            var partitionId = Tenant.PartitionIdFormat();
            return (await memoryDataRepository.GetAsync(id, partitionId, required)).DataJsonToObject<Tenant>();
        }

        public override async ValueTask<Track> GetTrackByNameAsync(Track.IdKey idKey, bool required = true, TelemetryScopedLogger scopedLogger = null)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            var id = await Track.IdFormatAsync(idKey);
            var partitionId = Track.PartitionIdFormat(idKey);
            return (await memoryDataRepository.GetAsync(id, partitionId, required)).DataJsonToObject<Track>();
        }

        public override async ValueTask<(List<T> items, string continuationToken)> GetListAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, int maxItemCount = 50, string continuationToken = null, TelemetryScopedLogger scopedLogger = null)
        {
            var partitionId = PartitionIdFormat<T>(idKey);

            var dataItems = (await memoryDataRepository.GetListAsync(partitionId, maxItemCount)).Select(i => i.DataJsonToObject<T>());
            if (whereQuery == null)
            {
                return (dataItems.ToList(), continuationToken);
            }
            else
            {
                var lambda = whereQuery.Compile();
                return (dataItems.Where(d => lambda(d)).ToList(), continuationToken);
            }
        }

        public override async ValueTask CreateAsync<T>(T item, TelemetryScopedLogger scopedLogger = null)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.SetTenantPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            await memoryDataRepository.CreateAsync(item.Id, item.PartitionId, item.ToJson());
        }

        public override async ValueTask UpdateAsync<T>(T item, TelemetryScopedLogger scopedLogger = null)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.SetTenantPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            await memoryDataRepository.UpdateAsync(item.Id, item.PartitionId, item.ToJson());
        }

        public override async ValueTask SaveAsync<T>(T item, TelemetryScopedLogger scopedLogger = null)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.SetTenantPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            await memoryDataRepository.SaveAsync(item.Id, item.ToJson());
        }

        public override async ValueTask DeleteAsync<T>(string id, TelemetryScopedLogger scopedLogger = null)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            await memoryDataRepository.DeleteAsync(id);
        }

        //public override ValueTask<T> DeleteAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null, TelemetryScopedLogger scopedLogger = null)
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
                var ids = await memoryDataRepository.GetListIdsAsync(partitionId);
                foreach (var id in ids)
                {
                    await memoryDataRepository.DeleteAsync(id);
                }
                return ids.Count();
            }
            else
            {
                var dataItems = (await memoryDataRepository.GetListAsync(partitionId)).Select(i => i.DataJsonToObject<T>());
                var lambda = whereQuery.Compile();
                var deleteItems = dataItems.Where(d => lambda(d));
                foreach (var item in deleteItems)
                {
                    await memoryDataRepository.DeleteAsync(item.Id);
                }
                return deleteItems.Count();
            }
        }
    }
}
