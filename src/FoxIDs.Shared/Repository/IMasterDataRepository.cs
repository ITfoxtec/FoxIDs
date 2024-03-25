using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FoxIDs.Models;

namespace FoxIDs.Repository
{
    public interface IMasterDataRepository
    {
        Task<bool> ExistsAsync<T>(string id) where T : MasterDocument;

        Task<int> CountAsync<T>(Expression<Func<T, bool>> whereQuery = null) where T : MasterDocument;

        Task<T> GetAsync<T>(string id, bool required = true) where T : MasterDocument;
        Task<HashSet<T>> GetListAsync<T>(Expression<Func<T, bool>> whereQuery = null, int maxItemCount = 50) where T : MasterDocument;

        Task CreateAsync<T>(T item) where T : MasterDocument;
        Task UpdateAsync<T>(T item) where T : MasterDocument;
        //Task SaveAsync<T>(T item) where T : MasterDocument;
        //Task DeleteAsync<T>(T item) where T : MasterDocument;
        Task SaveBulkAsync<T>(List<T> items) where T : MasterDocument;

        Task<T> DeleteAsync<T>(string id) where T : MasterDocument;
        Task DeleteBulkAsync<T>(List<string> ids) where T : MasterDocument;
    }
}