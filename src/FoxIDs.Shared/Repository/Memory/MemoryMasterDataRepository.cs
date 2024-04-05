//using ITfoxtec.Identity;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Threading.Tasks;

//namespace FoxIDs.Repository
//{
//    public class MemoryMasterDataRepository : MasterDataRepositoryBase
//    {
//        private readonly MemoryDataRepository memoryDataRepository;

//        public MemoryMasterDataRepository(MemoryDataRepository memoryDataRepository)
//        {
//            this.memoryDataRepository = memoryDataRepository;
//        }

//        public override ValueTask<bool> ExistsAsync<T>(string id) 
//        {
//            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

//            var partitionId = id.IdToMasterPartitionId();
//            return memoryDataRepository.ExistsAsync(id, partitionId);
//        }

//        public override async ValueTask<long> CountAsync<T>(Expression<Func<T, bool>> whereQuery = null) 
//        {
//            var partitionId = IdToMasterPartitionId<T>();
//            if (whereQuery == null)
//            {
//                return await memoryDataRepository.CountAsync(partitionId);
//            }
//            else
//            {
//                var dataItems = (await memoryDataRepository.GetListAsync(partitionId)).Select(i => i.DataJsonToObject<T>());
//                var lambda = whereQuery.Compile();
//                return dataItems.Where(d => lambda(d)).Count();
//            }
//        }

//        public override async ValueTask<T> GetAsync<T>(string id, bool required = true)
//        {
//            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

//            var partitionId = id.IdToMasterPartitionId();
//            return (await memoryDataRepository.GetAsync(id, partitionId, required)).DataJsonToObject<T>();
//        }

//        public override async ValueTask<List<T>> GetListAsync<T>(Expression<Func<T, bool>> whereQuery = null, int maxItemCount = 50) 
//        {
//            var partitionId = IdToMasterPartitionId<T>();
//            var dataItems = (await memoryDataRepository.GetListAsync(partitionId, maxItemCount)).Select(i => i.DataJsonToObject<T>());
//            if (whereQuery == null)
//            {
//                return dataItems.ToList();
//            }
//            else
//            {
//                var lambda = whereQuery.Compile();
//                return dataItems.Where(d => lambda(d)).ToList();
//            }
//        }

//        public override async ValueTask CreateAsync<T>(T item) 
//        {
//            if (item == null) new ArgumentNullException(nameof(item));
//            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

//            item.PartitionId = item.Id.IdToMasterPartitionId();
//            item.SetDataType();
//            await item.ValidateObjectAsync();

//            await memoryDataRepository.CreateAsync(item.Id, item.PartitionId, item.ToJson());
//        }

//        public override async ValueTask UpdateAsync<T>(T item)
//        {
//            if (item == null) new ArgumentNullException(nameof(item));
//            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

//            item.PartitionId = item.Id.IdToMasterPartitionId();
//            item.SetDataType();
//            await item.ValidateObjectAsync();

//            await memoryDataRepository.UpdateAsync(item.Id, item.PartitionId, item.ToJson());
//        }

//        public override async ValueTask SaveAsync<T>(T item)
//        {
//            if (item == null) new ArgumentNullException(nameof(item));
//            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

//            item.PartitionId = item.Id.IdToMasterPartitionId();
//            item.SetDataType();
//            await item.ValidateObjectAsync();

//            await memoryDataRepository.SaveAsync(item.Id, item.ToJson());
//        }

//        public override async ValueTask DeleteAsync<T>(T item)
//        {
//            if (item == null) new ArgumentNullException(nameof(item));
//            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

//            await item.ValidateObjectAsync();

//            await memoryDataRepository.DeleteAsync(item.Id);
//        }

//        public override async ValueTask SaveBulkAsync<T>(List<T> items)
//        {
//            if (items?.Count <= 0) new ArgumentNullException(nameof(items));
//            var firstItem = items.First();
//            if (firstItem.Id.IsNullOrEmpty()) throw new ArgumentNullException($"First item {nameof(firstItem.Id)}.", items.GetType().Name);

//            var partitionId = firstItem.Id.IdToMasterPartitionId();
//            foreach (var item in items)
//            {
//                item.PartitionId = partitionId;
//                item.SetDataType();
//                await item.ValidateObjectAsync();
//            }

//            foreach (var item in items)
//            {
//                await memoryDataRepository.SaveAsync(item.Id, item.ToJson());
//            }
//        }

//        public override async ValueTask DeleteAsync<T>(string id)
//        {
//            await memoryDataRepository.DeleteAsync(id);
//        }

//        public override async ValueTask DeleteBulkAsync<T>(List<string> ids)
//        {
//            foreach (string id in ids)
//            {
//                await memoryDataRepository.DeleteAsync(id);
//            }
//        }
//    }
//}
