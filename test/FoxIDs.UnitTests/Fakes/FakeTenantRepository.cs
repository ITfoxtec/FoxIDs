using FoxIDs.Models;
using FoxIDs.Repository;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FoxIDs.UnitTests.Mocks
{
    public class FakeTenantRepository : ITenantRepository
    {

        public Task<bool> ExistsAsync<T>(string id) where T : IDataDocument
        {
            if(id == "user:testtenant:testtrack:a2@test.com")
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<T> GetAsync<T>(string id, bool requered = true, bool delete = false) where T : IDataDocument
        {
            throw new NotImplementedException();
        }

        public Task<Tenant> GetTenantByNameAsync(string tenantName, bool requered = true)
        {
            throw new NotImplementedException();
        }

        public Task<Track> GetTrackByNameAsync(Track.IdKey idKey, bool requered = true)
        {
            throw new NotImplementedException();
        }

        public Task<UpParty> GetUpPartyByNameAsync(Party.IdKey idKey, bool requered = true)
        {
            throw new NotImplementedException();
        }

        public Task<DownParty> GetDownPartyByNameAsync(Party.IdKey idKey, bool requered = true)
        {
            throw new NotImplementedException();
        }

        public Task<HashSet<T>> GetListAsync<T>(Track.IdKey idKey = null, Expression<Func<T, bool>> whereQuery = null, int maxItemCount = 10) where T : IDataDocument
        {
            throw new NotImplementedException();
        }

        public Task CreateAsync<T>(T item) where T : IDataDocument
        {
            return Task.CompletedTask;
        }

        public Task UpdateAsync<T>(T item) where T : IDataDocument
        {
            throw new NotImplementedException();
        }

        public Task SaveAsync<T>(T item) where T : IDataDocument
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync<T>(string id) where T : IDataDocument
        {
            throw new NotImplementedException();
        }

        public Task<T> DeleteAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null) where T : IDataDocument
        {
            throw new NotImplementedException();
        }

        public Task<int> DeleteListAsync<T>(Track.IdKey idKey, Expression<Func<T, bool>> whereQuery = null) where T : IDataDocument
        {
            throw new NotImplementedException();
        }
    }
}
