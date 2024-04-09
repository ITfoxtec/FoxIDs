using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FoxIDs.Models;

namespace FoxIDs.Repository
{
    public interface IMasterDataRepository
    {
        ValueTask<bool> ExistsAsync<T>(string id) where T : MasterDocument;

        ValueTask<long> CountAsync<T>(Expression<Func<T, bool>> whereQuery = null) where T : MasterDocument;

        ValueTask<T> GetAsync<T>(string id, bool required = true) where T : MasterDocument;
        ValueTask<IReadOnlyCollection<T>> GetListAsync<T>(Expression<Func<T, bool>> whereQuery = null, int pageSize = Constants.Models.ListPageSize) where T : MasterDocument;

        ValueTask CreateAsync<T>(T item) where T : MasterDocument;
        ValueTask UpdateAsync<T>(T item) where T : MasterDocument;
        //ValueTask SaveAsync<T>(T item) where T : MasterDocument;
        //ValueTask DeleteAsync<T>(T item) where T : MasterDocument;
        ValueTask SaveBulkAsync<T>(IReadOnlyCollection<T> items) where T : MasterDocument;

        ValueTask DeleteAsync<T>(string id) where T : MasterDocument;
        ValueTask DeleteBulkAsync<T>(IReadOnlyCollection<string> ids) where T : MasterDocument;
    }
}