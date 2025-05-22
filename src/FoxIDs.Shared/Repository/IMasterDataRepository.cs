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
        ValueTask<IReadOnlyCollection<T>> GetManyAsync<T>(Expression<Func<T, bool>> whereQuery = null, int pageSize = Constants.Models.ListPageSize) where T : MasterDocument;

        /// <summary>
        /// Create document. Throws exception if already exists.
        /// </summary>
        ValueTask CreateAsync<T>(T item) where T : MasterDocument;
        /// <summary>
        /// Update document. Throws exception if not exists.
        /// </summary>
        ValueTask UpdateAsync<T>(T item) where T : MasterDocument;
        //ValueTask SaveAsync<T>(T item) where T : MasterDocument;
        //ValueTask DeleteAsync<T>(T item) where T : MasterDocument;
        /// <summary>
        /// Create or update many documents.
        /// </summary>
        ValueTask SaveManyAsync<T>(IReadOnlyCollection<T> items) where T : MasterDocument;

        /// <summary>
        /// Delete document. Throws exception if not already exists.
        /// </summary>
        ValueTask DeleteAsync<T>(string id) where T : MasterDocument;
        ValueTask DeleteManyAsync<T>(IReadOnlyCollection<string> ids) where T : MasterDocument;
        ValueTask DeleteManyAsync<T>() where T : MasterDocument;
    }
}