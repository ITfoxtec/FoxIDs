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
        ValueTask<bool> ExistsAsync<T>(string id, bool queryAdditionalIds = false, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;

        ValueTask<long> CountAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, bool usePartitionId = true) where T : IDataDocument;

        ValueTask<T> GetAsync<T>(string id, bool required = true, bool delete = false, bool queryAdditionalIds = false, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;
        ValueTask<Tenant> GetTenantByNameAsync(string tenantName, bool required = true, TelemetryScopedLogger scopedLogger = null);
        ValueTask<Track> GetTrackByNameAsync(Track.IdKey idKey, bool required = true, TelemetryScopedLogger scopedLogger = null);

        ValueTask<(IReadOnlyCollection<T> items, string paginationToken)> GetListAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, int pageSize = Constants.Models.ListPageSize, string paginationToken = null, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;

        /// <summary>
        /// Create document. Throws exception if already exists.
        /// </summary>
        ValueTask CreateAsync<T>(T item, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;
        /// <summary>
        /// Update document. Throws exception if not exists.
        /// </summary>
        ValueTask UpdateAsync<T>(T item, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;
        /// <summary>
        /// Create or update document.
        /// </summary>
        ValueTask SaveAsync<T>(T item, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;
        /// <summary>
        /// Create or update many documents.
        /// </summary>
        ValueTask SaveBulkAsync<T>(IReadOnlyCollection<T> items, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;

        /// <summary>
        /// Delete document. Throws exception if not already exists.
        /// </summary>
        ValueTask DeleteAsync<T>(string id, bool queryAdditionalIds = false, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;
        //ValueTask DeleteAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;
        ValueTask<long> DeleteListAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;
    }
}