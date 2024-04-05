using FoxIDs.Infrastructure;
using FoxIDs.Models;
using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FoxIDs.Repository
{
    public class FileTenantDataRepository : TenantDataRepositoryBase
    {
        private readonly FileDataRepository fileDataRepository;

        public FileTenantDataRepository(FileDataRepository fileDataRepository)
        {
            this.fileDataRepository = fileDataRepository;
        }

        public override ValueTask<bool> ExistsAsync<T>(string id, TelemetryScopedLogger scopedLogger = null)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = id.IdToTenantPartitionId();
            return fileDataRepository.ExistsAsync(id, partitionId);
        }

        public override async ValueTask<long> CountAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, bool usePartitionId = true)  
        {
            var partitionId = usePartitionId ? PartitionIdFormat<T>(idKey) : null;
            if (whereQuery == null)
            {
                return await fileDataRepository.CountAsync(partitionId, GetDataType<T>());
            }
            else
            {
                var dataItems = (await fileDataRepository.GetListAsync(partitionId, GetDataType<T>())).Select(i => i.DataJsonToObject<T>());
                var lambda = whereQuery.Compile();
                return dataItems.Where(d => lambda(d)).Count();
            }
        }

        public override async ValueTask<T> GetAsync<T>(string id, bool required = true, bool delete = false, TelemetryScopedLogger scopedLogger = null)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));

            var partitionId = id.IdToTenantPartitionId();
            return (await fileDataRepository.GetAsync(id, partitionId, required, delete)).DataJsonToObject<T>();
        }

        public override async ValueTask<Tenant> GetTenantByNameAsync(string tenantName, bool required = true, TelemetryScopedLogger scopedLogger = null)
        {
            if (tenantName.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(tenantName));

            var id = await Tenant.IdFormatAsync(tenantName);
            var partitionId = Tenant.PartitionIdFormat();
            return (await fileDataRepository.GetAsync(id, partitionId, required)).DataJsonToObject<Tenant>();
        }

        public override async ValueTask<Track> GetTrackByNameAsync(Track.IdKey idKey, bool required = true, TelemetryScopedLogger scopedLogger = null)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            var id = await Track.IdFormatAsync(idKey);
            var partitionId = Track.PartitionIdFormat(idKey);
            return (await fileDataRepository.GetAsync(id, partitionId, required)).DataJsonToObject<Track>();
        }

        public override async ValueTask<(List<T> items, string continuationToken)> GetListAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, int maxItemCount = 50, string continuationToken = null, TelemetryScopedLogger scopedLogger = null)
        {
            var partitionId = PartitionIdFormat<T>(idKey);
            var dataItems = (await fileDataRepository.GetListAsync(partitionId, GetDataType<T>(), maxItemCount)).Select(i => i.DataJsonToObject<T>());
            continuationToken = null;
            if (whereQuery == null)
            {
                return (dataItems.ToList(), continuationToken);
            }
            else
            {
                var lambda = whereQuery.Compile();
                return (dataItems.Where(d => lambda(d)).ToList(), continuationToken);
            }
        }

        public override async ValueTask CreateAsync<T>(T item, TelemetryScopedLogger scopedLogger = null)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.SetTenantPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            await fileDataRepository.CreateAsync(item.Id, item.PartitionId, item.ToJson());
        }

        public override async ValueTask UpdateAsync<T>(T item, TelemetryScopedLogger scopedLogger = null)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.SetTenantPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            await fileDataRepository.UpdateAsync(item.Id, item.PartitionId, item.ToJson());
        }

        public override async ValueTask SaveAsync<T>(T item, TelemetryScopedLogger scopedLogger = null)
        {
            if (item == null) new ArgumentNullException(nameof(item));
            if (item.Id.IsNullOrEmpty()) throw new ArgumentNullException(nameof(item.Id), item.GetType().Name);

            item.SetTenantPartitionId();
            item.SetDataType();
            await item.ValidateObjectAsync();

            await fileDataRepository.SaveAsync(item.Id, item.PartitionId, item.ToJson());
        }

        public override async ValueTask DeleteAsync<T>(string id, TelemetryScopedLogger scopedLogger = null)
        {
            if (id.IsNullOrWhiteSpace()) new ArgumentNullException(nameof(id));
            
            var partitionId = id.IdToTenantPartitionId();
            await fileDataRepository.DeleteAsync(id, partitionId);
        }

        //public override ValueTask<T> DeleteAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null, TelemetryScopedLogger scopedLogger = null)
        //{
        //    throw new NotImplementedException();
        //}

        public override async ValueTask<long> DeleteListAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null, TelemetryScopedLogger scopedLogger = null)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            await idKey.ValidateObjectAsync();
            var partitionId = PartitionIdFormat<T>(idKey);

            if (whereQuery == null)
            {
                return await fileDataRepository.DeleteListAsync(partitionId, GetDataType<T>());
            }
            else
            {
                var dataItems = (await fileDataRepository.GetListAsync(partitionId, GetDataType<T>())).Select(i => i.DataJsonToObject<T>());
                var lambda = whereQuery.Compile();
                var deleteItems = dataItems.Where(d => lambda(d));
                foreach (var item in deleteItems)
                {
                    await fileDataRepository.DeleteAsync(item.Id, item.PartitionId);
                }
                return deleteItems.Count();
            }
        }

        private string GetDataType<T>() where T : IDataDocument
        {
            var type = typeof(T);
            if (type == typeof(Tenant))
            {
                return Constants.Models.DataType.Tenant;
            }
            else if (type == typeof(Track))
            {
                return Constants.Models.DataType.Track;
            }
            else if (type == typeof(Party))
            {
                return Constants.Models.DataType.Party;
            }
            else if (type == typeof(UpParty))
            {
                return Constants.Models.DataType.UpParty;
            }
            else if (type == typeof(DownParty))
            {
                return Constants.Models.DataType.DownParty;
            }
            else if (type == typeof(User))
            {
                return Constants.Models.DataType.User;
            }
            else if (type == typeof(UserControlProfile))
            {
                return Constants.Models.DataType.UserControlProfile;
            }
            else if (type == typeof(ExternalUser))
            {
                return Constants.Models.DataType.ExternalUser;
            }
            else if (type == typeof(ExternalUser))
            {
                return Constants.Models.DataType.ExternalUser;
            }
            else if (type == typeof(AuthCodeTtlGrant))
            {
                return Constants.Models.DataType.AuthCodeTtlGrant;
            }
            else if (type == typeof(RefreshTokenGrant) || type == typeof(RefreshTokenTtlGrant))
            {
                return Constants.Models.DataType.RefreshTokenGrant;
            }           
            else
            { 
                throw new NotSupportedException($"Type '{type}'.");
            }
        }
    }
}
