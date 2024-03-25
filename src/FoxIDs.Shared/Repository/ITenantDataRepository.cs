using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FoxIDs.Infrastructure;
using FoxIDs.Models;

namespace FoxIDs.Repository
{
    public interface ITenantDataRepository
    {
        Task<bool> ExistsAsync<T>(string id, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;

        Task<int> CountAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, bool usePartitionId = true) where T : IDataDocument;

        Task<T> GetAsync<T>(string id, bool required = true, bool delete = false, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;
        Task<Tenant> GetTenantByNameAsync(string tenantName, bool required = true, TelemetryScopedLogger scopedLogger = null);
        Task<Track> GetTrackByNameAsync(Track.IdKey idKey, bool required = true, TelemetryScopedLogger scopedLogger = null);

        Task<(HashSet<T> items, string continuationToken)> GetListAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, int maxItemCount = 50, string continuationToken = null, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;

        /// <summary>
        /// Create document. Throws exception if already exists.
        /// </summary>
        Task CreateAsync<T>(T item, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;
        /// <summary>
        /// Update document. Throws exception if not exists.
        /// </summary>
        Task UpdateAsync<T>(T item, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;
        /// <summary>
        /// Create or update document.
        /// </summary>
        Task SaveAsync<T>(T item, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;
        /// <summary>
        /// Delete document. Throws exception if not already exists.
        /// </summary>
        Task<T> DeleteAsync<T>(string id, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;
        //Task<T> DeleteAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;
        Task<int> DeleteListAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;
    }
}