using FoxIDs.Infrastructure;
using FoxIDs.Logic;
using FoxIDs.Models;
using ITfoxtec.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Wololo.PgKeyValueDB;

namespace FoxIDs.Repository
{
    public class PgMasterDataRepository : MasterDataRepositoryBase
    {
        private readonly PgKeyValueDB db;
        private readonly AuditLogic auditLogic;

        public PgMasterDataRepository([FromKeyedServices(Constants.Models.DataType.Master)] PgKeyValueDB db, AuditLogic auditLogic)
        {
            this.db = db ?? throw new ArgumentNullException(nameof(db));
            this.auditLogic = auditLogic ?? throw new ArgumentNullException(nameof(auditLogic));
        }

        public override async ValueTask<bool> ExistsAsync<T>(string id)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = id.IdToMasterPartitionId();
            return await db.ExistsAsync(id, partitionId);
        }

        public override async ValueTask<long> CountAsync<T>(Expression<Func<T, bool>> whereQuery = null)
        {
            var partitionId = TypeToMasterPartitionId<T>();
            return (int) await db.CountAsync(partitionId, whereQuery);
        }

        public override async ValueTask<T> GetAsync<T>(string id, bool required = true)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = id.IdToMasterPartitionId();
            var item = await db.GetAsync<T>(id, partitionId);
            if (required && item == null)
            { 
                throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };
            }
            await item.ValidateObjectAsync();
            return item;
        }

        public override async ValueTask<IReadOnlyCollection<T>> GetManyAsync<T>(Expression<Func<T, bool>> whereQuery = null, int pageSize = Constants.Models.ListPageSize)
        {
            var partitionId = TypeToMasterPartitionId<T>();
            var dataItems = await db.GetListAsync(partitionId, whereQuery, pageSize).ToListAsync();
            var items = dataItems.ToList();
            await items.ValidateObjectAsync();
            return items;
        }

        public override async ValueTask CreateAsync<T>(T item)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.PartitionId = item.Id.IdToMasterPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            var shouldAudit = auditLogic.ShouldLogAuditData();
            var created = await db.CreateAsync(item.Id, item, item.PartitionId);
            if (!created)
            {
                throw new FoxIDsDataException(item.Id, item.PartitionId) { StatusCode = DataStatusCode.Conflict };
            }

            if (shouldAudit)
            {
                auditLogic.LogDataEvent(AuditDataActions.Create, default, item, item.Id);
            }
        }

        public override async ValueTask UpdateAsync<T>(T item)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.PartitionId = item.Id.IdToMasterPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            var shouldAudit = auditLogic.ShouldLogAuditData();
            T existing = default;
            if (shouldAudit)
            {
                existing = await db.GetAsync<T>(item.Id, item.PartitionId);
                if (existing == null)
                {
                    throw new FoxIDsDataException(item.Id, item.PartitionId) { StatusCode = DataStatusCode.NotFound };
                }
            }

            if(!await db.UpdateAsync(item.Id, item, item.PartitionId))
            {
                throw new FoxIDsDataException(item.Id, item.PartitionId) { StatusCode = DataStatusCode.NotFound };
            }

            if (shouldAudit)
            {
                auditLogic.LogDataEvent(AuditDataActions.Update, existing, item, item.Id);
            }
        }

        public override async ValueTask SaveAsync<T>(T item)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.PartitionId = item.Id.IdToMasterPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            var shouldAudit = auditLogic.ShouldLogAuditData();
            T existing = default;
            if (shouldAudit)
            {
                existing = await db.GetAsync<T>(item.Id, item.PartitionId);
            }

            await db.UpsertAsync(item.Id, item, item.PartitionId);

            if (shouldAudit)
            {
                auditLogic.LogDataEvent(AuditDataActions.Save, existing, item, item.Id);
            }
        }

        public override async ValueTask DeleteAsync<T>(T item)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.PartitionId = item.Id.IdToMasterPartitionId();
            await item.ValidateObjectAsync();

            var shouldAudit = auditLogic.ShouldLogAuditData();
            await db.RemoveAsync(item.Id, item.PartitionId);

            if (shouldAudit)
            {
                auditLogic.LogDataEvent(AuditDataActions.Delete, item, default, item.Id);
            }
        }

        public override async ValueTask SaveManyAsync<T>(IReadOnlyCollection<T> items)
        {
            if (items?.Count <= 0) new ArgumentNullException(nameof(items));
            var firstItem = items.First();
            if (firstItem.Id.IsNullOrEmpty()) throw new ArgumentNullException($"First item {nameof(firstItem.Id)}.", items.GetType().Name);

            var partitionId = firstItem.Id.IdToMasterPartitionId();
            foreach (var item in items)
            {
                item.PartitionId = partitionId;
                item.SetDataType();
                await item.ValidateObjectAsync();
            }

            foreach (var item in items)
            {
                await SaveAsync(item);
            }
        }

        public override async ValueTask DeleteAsync<T>(string id)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = id.IdToMasterPartitionId();

            var shouldAudit = auditLogic.ShouldLogAuditData();
            T existing = default;
            if (shouldAudit)
            {
                existing = await db.GetAsync<T>(id, partitionId);
                if (existing == null)
                {
                    throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };
                }
            }

            if(!await db.RemoveAsync(id, partitionId))
            {
                throw new FoxIDsDataException(id, partitionId) { StatusCode = DataStatusCode.NotFound };
            }

            if (shouldAudit)
            {
                auditLogic.LogDataEvent(AuditDataActions.Delete, existing, default, id);
            }
        }

        public override async ValueTask DeleteManyAsync<T>(IReadOnlyCollection<string> ids)
        {
            var shouldAudit = auditLogic.ShouldLogAuditData();
            foreach (string id in ids)
            {
                var partitionId = id.IdToMasterPartitionId();
                T existing = default;
                if (shouldAudit)
                {
                    existing = await db.GetAsync<T>(id, partitionId);
                }

                var removed = await db.RemoveAsync(id, partitionId);
                if (shouldAudit && removed && existing != null)
                {
                    auditLogic.LogDataEvent(AuditDataActions.Delete, existing, default, id);
                }
            }
        }

        public override ValueTask DeleteManyAsync<T>()
        {
            throw new NotSupportedException("Not supported by PostgreSql.");
        }
    }
}
