using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FoxIDs.Models;

namespace FoxIDs.Repository
{
    public interface ITenantRepository
    {
        Task<bool> ExistsAsync<T>(string id) where T : IDataDocument;
        Task<T> GetAsync<T>(string id, bool requered = true, bool delete = false) where T : IDataDocument;
        Task<Tenant> GetTenantByNameAsync(string tenantName, bool requered = true);
        Task<Track> GetTrackByNameAsync(Track.IdKey idKey, bool requered = true);
        Task<UpParty> GetUpPartyByNameAsync(Party.IdKey idKey, bool requered = true);
        Task<DownParty> GetDownPartyByNameAsync(Party.IdKey idKey, bool requered = true);
        Task CreateAsync<T>(T item) where T : IDataDocument;
        Task SaveAsync<T>(T item) where T : IDataDocument;
        Task DeleteAsync<T>(string id) where T : IDataDocument;
        Task<T> DeleteAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery) where T : IDataDocument;
    }
}