using FoxIDs.Models;
using FoxIDs.Repository;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FoxIDs.UnitTests.Mocks
{
    public class FakeMasterRepository : IMasterDataRepository
    {
        public ValueTask<bool> ExistsAsync<T>(string id) where T : MasterDocument
        {
            if (id == "prisk:@master:3357229DDDC9963302283F4D4863A74F310C9E80") // Password: !QAZ2wsx
            {
                return ValueTask.FromResult(true);
            }

            return ValueTask.FromResult(false);
        }

        public ValueTask<int> CountAsync<T>(Expression<Func<T, bool>> whereQuery = null) where T : MasterDocument
        {
            throw new NotImplementedException();
        }

        public ValueTask<T> GetAsync<T>(string id, bool required = true) where T : MasterDocument
        {
            throw new NotImplementedException();
        }

        public ValueTask<HashSet<T>> GetListAsync<T>(Expression<Func<T, bool>> whereQuery = null, int maxItemCount = 10) where T : MasterDocument
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

        public ValueTask SaveAsync<T>(T item) where T : MasterDocument
        {
            throw new NotImplementedException();
        }

        public ValueTask<T> DeleteAsync<T>(string id) where T : MasterDocument
        {
            throw new NotImplementedException();
        }

        public ValueTask DeleteAsync<T>(T item) where T : MasterDocument
        {
            throw new NotImplementedException();
        }

        public ValueTask SaveBulkAsync<T>(List<T> items) where T : MasterDocument
        {
            throw new NotImplementedException();
        }

        public ValueTask DeleteBulkAsync<T>(List<string> ids) where T : MasterDocument
        {
            throw new NotImplementedException();
        }
    }
}
