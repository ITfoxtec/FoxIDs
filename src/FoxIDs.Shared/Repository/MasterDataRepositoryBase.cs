using FoxIDs.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FoxIDs.Repository
{
    public abstract class MasterDataRepositoryBase : IMasterDataRepository
    {
        public abstract ValueTask<bool> ExistsAsync<T>(string id) where T : MasterDocument;
        public abstract ValueTask<long> CountAsync<T>(Expression<Func<T, bool>> whereQuery = null) where T : MasterDocument;
        public abstract ValueTask<T> GetAsync<T>(string id, bool required = true) where T : MasterDocument;
        public abstract ValueTask<List<T>> GetListAsync<T>(Expression<Func<T, bool>> whereQuery = null, int pageSize = Constants.Models.ListPageSize) where T : MasterDocument;
        public abstract ValueTask CreateAsync<T>(T item) where T : MasterDocument;
        public abstract ValueTask UpdateAsync<T>(T item) where T : MasterDocument;
        public abstract ValueTask SaveAsync<T>(T item) where T : MasterDocument;
        public abstract ValueTask DeleteAsync<T>(T item) where T : MasterDocument;
        public abstract ValueTask SaveBulkAsync<T>(List<T> items) where T : MasterDocument;
        public abstract ValueTask DeleteAsync<T>(string id) where T : MasterDocument;
        public abstract ValueTask DeleteBulkAsync<T>(List<string> ids) where T : MasterDocument;

        protected string IdToMasterPartitionId<T>() where T : MasterDocument
        {
            if (typeof(T) == typeof(Plan))
            {
                return Plan.PartitionIdFormat(new MasterDocument.IdKey());
            }
            else if (typeof(T) == typeof(RiskPassword))
            {
                return RiskPassword.PartitionIdFormat(new MasterDocument.IdKey());
            }
            else
            {
                return MasterDocument.PartitionIdFormat(new MasterDocument.IdKey());
            }

        }
    }
}
