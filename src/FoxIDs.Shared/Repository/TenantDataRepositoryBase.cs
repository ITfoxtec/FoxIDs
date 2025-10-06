using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FoxIDs.Repository
{
    public abstract class TenantDataRepositoryBase : ITenantDataRepository
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        protected TenantDataRepositoryBase(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        protected HttpContext HttpContext => httpContextAccessor?.HttpContext;

        public abstract ValueTask<bool> ExistsAsync<T>(string id, bool queryAdditionalIds = false, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;
        public abstract ValueTask<long> CountAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, bool usePartitionId = true) where T : IDataDocument;
        public abstract ValueTask<T> GetAsync<T>(string id, bool required = true, bool delete = false, bool queryAdditionalIds = false, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;
        public abstract ValueTask<Tenant> GetTenantByNameAsync(string tenantName, bool required = true, TelemetryScopedLogger scopedLogger = null);
        public abstract ValueTask<Track> GetTrackByNameAsync(Track.IdKey idKey, bool required = true, TelemetryScopedLogger scopedLogger = null);
        public abstract ValueTask<(IReadOnlyCollection<T> items, string paginationToken)> GetManyAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, int pageSize = Constants.Models.ListPageSize, string paginationToken = null, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;
        public abstract ValueTask CreateAsync<T>(T item, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;
        public abstract ValueTask UpdateAsync<T>(T item, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;
        public abstract ValueTask SaveAsync<T>(T item, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;
        public abstract ValueTask SaveManyAsync<T>(IReadOnlyCollection<T> items, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;
        public abstract ValueTask DeleteAsync<T>(string id, bool queryAdditionalIds = false, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;
        //public abstract ValueTask DeleteAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;
        public abstract ValueTask<long> DeleteManyAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;
        public abstract ValueTask DeleteManyAsync<T>(IReadOnlyCollection<string> ids, bool queryAdditionalIds = false, TelemetryScopedLogger scopedLogger = null) where T : IDataDocument;

        protected string PartitionIdFormat<T>(Track.IdKey idKey) where T : IDataDocument
        {
            if (typeof(T).Equals(typeof(Tenant)))
            {
                return Tenant.PartitionIdFormat();
            }
            else if (typeof(T).Equals(typeof(Used)))
            {
                return Used.PartitionIdFormat();
            }
            else if (typeof(T).Equals(typeof(Track)))
            {
                if (idKey == null) new ArgumentNullException(nameof(idKey));
                return Track.PartitionIdFormat(idKey);
            }
            else
            {
                if (idKey == null) new ArgumentNullException(nameof(idKey));
                return DataDocument.PartitionIdFormat(idKey);
            }
        }

        protected TelemetryScopedLogger GetScopedLogger(TelemetryScopedLogger scopedLogger = null)
        {
            return scopedLogger ?? HttpContext?.RequestServices.GetService<TelemetryScopedLogger>();
        }
    }
}
