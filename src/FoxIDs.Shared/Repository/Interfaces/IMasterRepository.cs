using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FoxIDs.Models;

namespace FoxIDs.Repository
{
    public interface IMasterRepository
    {
        Task<bool> ExistsAsync<T>(string id) where T : MasterDocument;

        Task<int> CountAsync<T>(Expression<Func<T, bool>> whereQuery = null) where T : MasterDocument;

        Task<T> GetAsync<T>(string id, bool requered = true) where T : MasterDocument;
        //Task<FeedResponse<TResult>> GetQueryAsync<T, TResult>(T item, Expression<Func<T, bool>> whereQuery, Expression<Func<T, TResult>> selector) where T : MasterDocument;
        //Task<FeedResponse<TResult>> GetQueryAsync<T, TResult>(string partitionId, Expression<Func<T, bool>> whereQuery, Expression<Func<T, TResult>> selector) where T : MasterDocument;
        //Task<int> GetQueryCountAsync<T>(T item, Expression<Func<T, bool>> whereQuery) where T : MasterDocument;
        //Task<int> GetQueryCountAsync<T>(string partitionId, Expression<Func<T, bool>> whereQuery) where T : MasterDocument;
        Task SaveAsync<T>(T item) where T : MasterDocument;
        Task SaveBulkAsync<T>(List<T> items) where T : MasterDocument;
        Task DeleteAsync<T>(T item) where T : MasterDocument;

    }
}