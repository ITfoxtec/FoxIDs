using FoxIDs.Repository;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FoxIDs.UnitTests.Mocks
{
    public class FakeMasterRepository : MasterDataRepositoryBase
    {
        public override ValueTask<bool> ExistsAsync<T>(string id)
        {
            if (id == "prisk:@master:3357229DDDC9963302283F4D4863A74F310C9E80") // Password: !QAZ2wsx
            {
                return ValueTask.FromResult(true);
            }

            return ValueTask.FromResult(false);
        }

        public override ValueTask<long> CountAsync<T>(Expression<Func<T, bool>> whereQuery = null)
        {
            throw new NotImplementedException();
        }

        public override ValueTask<T> GetAsync<T>(string id, bool required = true)
        {
            throw new NotImplementedException();
        }

        public override ValueTask<IReadOnlyCollection<T>> GetManyAsync<T>(Expression<Func<T, bool>> whereQuery = null, int pageSize = 10)
        {
            throw new NotImplementedException();
        }

        public override ValueTask CreateAsync<T>(T item)
        {
            throw new NotImplementedException();
        }

        public override ValueTask UpdateAsync<T>(T item)
        {
            throw new NotImplementedException();
        }

        public override ValueTask SaveAsync<T>(T item)
        {
            throw new NotImplementedException();
        }

        public override ValueTask DeleteAsync<T>(string id)
        {
            throw new NotImplementedException();
        }

        public override ValueTask DeleteAsync<T>(T item)
        {
            throw new NotImplementedException();
        }

        public override ValueTask SaveManyAsync<T>(IReadOnlyCollection<T> items)
        {
            throw new NotImplementedException();
        }

        public override ValueTask DeleteManyAsync<T>(IReadOnlyCollection<string> ids)
        {
            throw new NotImplementedException();
        }

        public override ValueTask DeleteManyAsync<T>()
        {
            throw new NotImplementedException();
        }
    }
}
