using FoxIDs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FoxIDs.Repository
{
    public class MemoryDataRepository 
    {
        public Task<bool> ExistsAsync(string id) 
        {
            throw new NotImplementedException();
        }

        public Task<int> CountAsync(Expression<Func<string, bool>> whereQuery = null)
        {
            throw new NotImplementedException();
        }

        public Task GetAsync(string id, bool required = true)
        {
            throw new NotImplementedException();
        }

        public Task<HashSet<string>> GetListAsync(Expression<Func<string, bool>> whereQuery = null, int maxItemCount = 50)
        {
            throw new NotImplementedException();
        }

        public Task CreateAsync(string item)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(string item)
        {
            throw new NotImplementedException();
        }

        public Task SaveAsync(string item)
        {
            throw new NotImplementedException();
        }

        //public Task DeleteAsync(string item)
        //{
        //    throw new NotImplementedException();
        //}

        public Task SaveBulkAsync(List<string> items)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task DeleteBulkAsync(List<string> ids)
        {
            throw new NotImplementedException();
        }
    }
}
