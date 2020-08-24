using FoxIDs.Models;
using FoxIDs.Repository;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FoxIDs.UnitTests.Mocks
{
    public class FakeMasterRepository : IMasterRepository
    {
        public Task<bool> ExistsAsync<T>(string id) where T : MasterDocument
        {
            if (id == "prisk:@master:3357229DDDC9963302283F4D4863A74F310C9E80") // Password: !QAZ2wsx
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<int> CountAsync<T>(Expression<Func<T, bool>> whereQuery = null) where T : MasterDocument
        {
            throw new NotImplementedException();
        }

        public Task<T> GetAsync<T>(string id, bool requered = true) where T : MasterDocument
        {
            throw new NotImplementedException();
        }

        public Task SaveAsync<T>(T item) where T : MasterDocument
        {
            throw new NotImplementedException();
        }

        public Task SaveBulkAsync<T>(List<T> items) where T : MasterDocument
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync<T>(T item) where T : MasterDocument
        {
            throw new NotImplementedException();
        }
    }
}
