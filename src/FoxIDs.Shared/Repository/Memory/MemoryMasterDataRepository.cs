using FoxIDs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FoxIDs.Repository
{
    public class MemoryMasterDataRepository : IMasterDataRepository
    {
        public ValueTask<bool> ExistsAsync<T>(string id) where T : MasterDocument
        {
            throw new NotImplementedException();
        }

        public ValueTask<int> CountAsync<T>(Expression<Func<T, bool>> whereQuery = null) where T : MasterDocument
        {
            throw new NotImplementedException();
        }

        public ValueTask<T> GetAsync<T>(string id, bool required = true) where T : MasterDocument
        {
            throw new NotImplementedException();
        }

        public ValueTask<HashSet<T>> GetListAsync<T>(Expression<Func<T, bool>> whereQuery = null, int maxItemCount = 50) where T : MasterDocument
        {
            throw new NotImplementedException();
        }

        public ValueTask CreateAsync<T>(T item) where T : MasterDocument
        {
            throw new NotImplementedException();
        }

        public ValueTask UpdateAsync<T>(T item) where T : MasterDocument
        {
            throw new NotImplementedException();
        }

        //public ValueTask SaveAsync<T>(T item) where T : MasterDocument
        //{
        //    throw new NotImplementedException();
        //}

        //public ValueTask DeleteAsync<T>(T item) where T : MasterDocument
        //{
        //    throw new NotImplementedException();
        //}

        public ValueTask SaveBulkAsync<T>(List<T> items) where T : MasterDocument
        {
            throw new NotImplementedException();
        }

        public ValueTask<T> DeleteAsync<T>(string id) where T : MasterDocument
        {
            throw new NotImplementedException();
        }

        public ValueTask DeleteBulkAsync<T>(List<string> ids) where T : MasterDocument
        {
            throw new NotImplementedException();
        }
    }
}
