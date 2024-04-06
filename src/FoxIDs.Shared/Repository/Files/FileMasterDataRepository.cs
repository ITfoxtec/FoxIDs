using FoxIDs.Models;
using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FoxIDs.Repository
{
    public class FileMasterDataRepository : MasterDataRepositoryBase
    {
        private readonly FileDataRepository fileDataRepository;

        public FileMasterDataRepository(FileDataRepository fileDataRepository)
        {
            this.fileDataRepository = fileDataRepository;
        }

        public override ValueTask<bool> ExistsAsync<T>(string id)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = id.IdToMasterPartitionId();
            return fileDataRepository.ExistsAsync(id, partitionId);
        }

        public override async ValueTask<long> CountAsync<T>(Expression<Func<T, bool>> whereQuery = null)
        {
            var partitionId = IdToMasterPartitionId<T>();
            if (whereQuery == null)
            {
                return await fileDataRepository.CountAsync(partitionId, GetDataType<T>());
            }
            else
            {
                var dataItems = (await fileDataRepository.GetListAsync(partitionId, GetDataType<T>())).Select(i => i.DataJsonToObject<T>());
                var lambda = whereQuery.Compile();
                return dataItems.Where(d => lambda(d)).Count();
            }
        }

        public override async ValueTask<T> GetAsync<T>(string id, bool required = true)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = id.IdToMasterPartitionId();
            var item = (await fileDataRepository.GetAsync(id, partitionId, required)).DataJsonToObject<T>();
            await item.ValidateObjectAsync();
            return item;
        }

        public override async ValueTask<List<T>> GetListAsync<T>(Expression<Func<T, bool>> whereQuery = null, int maxItemCount = 50)
        {
            var partitionId = IdToMasterPartitionId<T>();
            var dataItems = (await fileDataRepository.GetListAsync(partitionId, GetDataType<T>(), maxItemCount)).Select(i => i.DataJsonToObject<T>());
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
        }

        public override async ValueTask CreateAsync<T>(T item)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.PartitionId = item.Id.IdToMasterPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            await fileDataRepository.CreateAsync(item.Id, item.PartitionId, item.ToJson());
        }

        public override async ValueTask UpdateAsync<T>(T item)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.PartitionId = item.Id.IdToMasterPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            await fileDataRepository.UpdateAsync(item.Id, item.PartitionId, item.ToJson());
        }

        public override async ValueTask SaveAsync<T>(T item)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.PartitionId = item.Id.IdToMasterPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            await fileDataRepository.SaveAsync(item.Id, item.PartitionId, item.ToJson());
        }

        public override async ValueTask DeleteAsync<T>(T item)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.PartitionId = item.Id.IdToMasterPartitionId();
            await item.ValidateObjectAsync();

            await fileDataRepository.DeleteAsync(item.Id, item.PartitionId);
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
                await fileDataRepository.SaveAsync(item.Id, item.PartitionId, item.ToJson());
            }
        }

        public override async ValueTask DeleteAsync<T>(string id)
        {
            var partitionId = id.IdToMasterPartitionId();
            await fileDataRepository.DeleteAsync(id, partitionId);
        }

        public override async ValueTask DeleteBulkAsync<T>(List<string> ids)
        {
            foreach (string id in ids)
            {
                var partitionId = id.IdToMasterPartitionId();
                await fileDataRepository.DeleteAsync(id, partitionId);
            }
        }

        private string GetDataType<T>() where T : MasterDocument
        {
            var type = typeof(T);
            if (type == typeof(RiskPassword))
            {
                return Constants.Models.DataType.RiskPassword;
            }
            else if (type == typeof(Plan))
            {
                return Constants.Models.DataType.Plan;
            }
            else
            {
                throw new NotSupportedException($"Type '{type}'.");
            }
        }
    }
}
