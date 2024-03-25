using FoxIDs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FoxIDs.Repository
{
    public class FileMasterDataRepository : IMasterDataRepository
    {
        public Task<bool> ExistsAsync<T>(string id) where T : MasterDocument
        {
            throw new NotImplementedException();
        }

        public Task<int> CountAsync<T>(Expression<Func<T, bool>> whereQuery = null) where T : MasterDocument
        {
            throw new NotImplementedException();
        }

        public Task<T> GetAsync<T>(string id, bool required = true) where T : MasterDocument
        {
            throw new NotImplementedException();
        }

        public Task<HashSet<T>> GetListAsync<T>(Expression<Func<T, bool>> whereQuery = null, int maxItemCount = 50) where T : MasterDocument
        {
            throw new NotImplementedException();
        }

        public Task CreateAsync<T>(T item) where T : MasterDocument
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync<T>(T item) where T : MasterDocument
        {
            throw new NotImplementedException();
        }

        //public Task SaveAsync<T>(T item) where T : MasterDocument
        //{
        //    throw new NotImplementedException();
        //}

        //public Task DeleteAsync<T>(T item) where T : MasterDocument
        //{
        //    throw new NotImplementedException();
        //}

        public Task SaveBulkAsync<T>(List<T> items) where T : MasterDocument
        {
            throw new NotImplementedException();
        }

        public Task<T> DeleteAsync<T>(string id) where T : MasterDocument
        {
            throw new NotImplementedException();
        }

        public Task DeleteBulkAsync<T>(List<string> ids) where T : MasterDocument
        {
            throw new NotImplementedException();
        }
    }
}
