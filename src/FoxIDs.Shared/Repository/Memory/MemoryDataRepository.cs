using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Repository
{
    public class MemoryDataRepository 
    {
        private ConcurrentDictionary<string, (string, DateTimeOffset?)> dataRepository = new ConcurrentDictionary<string, (string, DateTimeOffset?)>();

        public async ValueTask<bool> ExistsAsync(string id, string partitionId) 
        {
            var data = await GetAsync(id, partitionId, required: false);
            return data != null;
        }

        public ValueTask<int> CountAsync(string partitionId)
        {
            return ValueTask.FromResult((partitionId == null ? dataRepository : dataRepository.Where(item => item.Key.StartsWith(partitionId))).Count());
        }

        public ValueTask<string> GetAsync(string id, string partitionId, bool required = true, bool delete = false)
        {
            if (dataRepository.ContainsKey(id))
            {
                (var data, var validUntil) = dataRepository[id];
                if (IsValid(validUntil))
                {
                    if (delete)
                    {
                        dataRepository.TryRemove(id, out _);
                    }
                    return ValueTask.FromResult(data);
                }
                else
                {
                    dataRepository.TryRemove(id, out _);
                }
            }

            if (required)
            {
                throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };
            }
            else
            {
                return ValueTask.FromResult<string>(null);
            }
        }

        public ValueTask<List<string>> GetListAsync(string partitionId, int? maxItemCount = null)
        {
            var values = (partitionId == null ? dataRepository : dataRepository.Where(item => item.Key.StartsWith(partitionId))).Select(item => item.Value);
            var result = new List<string>();
            var count = 0;
            foreach (var value in values)
            {
                if (IsValid(value.Item2))
                {
                    result.Add(value.Item1);
                    count++;
                    if (maxItemCount.HasValue && count >= maxItemCount.Value)
                    {
                        break;
                    }
                }
            }
            return ValueTask.FromResult(result);
        }

        public ValueTask<List<string>> GetListIdsAsync(string partitionId)
        {
            var items = (partitionId == null ? dataRepository : dataRepository.Where(item => item.Key.StartsWith(partitionId)));
            var ids = new List<string>();
            foreach (var item in items)
            {
                if (IsValid(item.Value.Item2))
                {
                    ids.Add(item.Key);
                }
            }
            return ValueTask.FromResult(ids);
        }

        public ValueTask CreateAsync(string id, string partitionId, string item)
        {
            if (dataRepository.ContainsKey(id))
            {
                throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.Conflict };
            }

            dataRepository[id] = (item, null);
            return ValueTask.CompletedTask;
        }

        public ValueTask UpdateAsync(string id, string partitionId, string item)
        {
            if (!dataRepository.ContainsKey(id))
            {
                throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };
            }

            dataRepository[id] = (item, null);
            return ValueTask.CompletedTask;
        }

        public ValueTask SaveAsync(string id, string item)
        {
            dataRepository[id] = (item, null);
            return ValueTask.CompletedTask;
        }

        public ValueTask<string> DeleteAsync(string id)
        {
            if (dataRepository.TryRemove(id, out var result))
            {
                return ValueTask.FromResult(result.Item1);
            }
            else
            {
                return ValueTask.FromResult<string>(null);
            }
        }

        private bool IsValid(DateTimeOffset? validUntil)
        {
            return validUntil == null || validUntil >= DateTimeOffset.UtcNow;
        }
    }
}
