using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FoxIDs.Repository
{
    public class MemoryDataRepository 
    {
        private ConcurrentDictionary<string, (string, DateTimeOffset)> dataRepository = new ConcurrentDictionary<string, (string, DateTimeOffset)>();

        public ValueTask<bool> ExistsAsync(string id) 
        {
            return ValueTask.FromResult(dataRepository.ContainsKey(id));
        }

        public ValueTask<int> CountAsync(Expression<Func<string, bool>> whereQuery = null)
        {
            throw new NotImplementedException();
        }

        public ValueTask GetAsync(string id, bool required = true)
        {
            throw new NotImplementedException();
        }

        public ValueTask<HashSet<string>> GetListAsync(Expression<Func<string, bool>> whereQuery = null, int maxItemCount = 50)
        {
            throw new NotImplementedException();
        }

        public ValueTask CreateAsync(string item)
        {
            throw new NotImplementedException();
        }

        public ValueTask UpdateAsync(string item)
        {
            throw new NotImplementedException();
        }

        public ValueTask SaveAsync(string item)
        {
            throw new NotImplementedException();
        }

        //public ValueTask DeleteAsync(string item)
        //{
        //    throw new NotImplementedException();
        //}

        public ValueTask SaveBulkAsync(List<string> items)
        {
            throw new NotImplementedException();
        }

        public ValueTask DeleteAsync(string id)
        {
            throw new NotImplementedException();
        }

        public ValueTask DeleteBulkAsync(List<string> ids)
        {
            throw new NotImplementedException();
        }
    }
}
