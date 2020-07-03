using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class DownPartyService : BaseService
    {
        private const string filterApiUri = "api/{tenant}/master/!filterdownparty";
        private const string oidcApiUri = "api/{tenant}/master/!oidcdownparty";
        private const string oauthApiUri = "api/{tenant}/master/!oauthdownparty";
        private const string samlApiUri = "api/{tenant}/master/!samldownparty";
        private readonly HttpClient httpClient;

        public DownPartyService(HttpClient httpClient, RouteBindingLogic routeBindingLogic) : base(routeBindingLogic)
        {
            this.httpClient = httpClient;
        }

        public async Task<IEnumerable<DownParty>> FilterDownPartyAsync(string filterName)
        {
            using var response = await httpClient.GetAsync($"{await GetTenantApiUrlAsync(filterApiUri)}?filterName={filterName}");
            var downParties = await response.ToObjectAsync<IEnumerable<DownParty>>();
            return downParties;
        }

        #region OidcDownParty
        public async Task<OidcDownParty> GetOidcDownPartyAsync(string name)
        {
            using var response = await httpClient.GetAsync($"{await GetTenantApiUrlAsync(oidcApiUri)}?name={name}");
            var oidcDownParties = await response.ToObjectAsync<OidcDownParty>();
            return oidcDownParties;
        }

        public async Task CreateOidcDownPartyAsync(OidcDownParty party)
        {
            using var response = await httpClient.PostAsJsonAsync(await GetTenantApiUrlAsync(oidcApiUri), party);
        }

        public async Task UpdateOidcDownPartyAsync(OidcDownParty party)
        {
            using var response = await httpClient.PutAsJsonAsync(await GetTenantApiUrlAsync(oidcApiUri), party);
        }

        public async Task DeleteOidcDownPartyAsync(string name)
        {
            await httpClient.DeleteAsync($"{await GetTenantApiUrlAsync(oidcApiUri)}?name={name}");
        }
        #endregion

        #region OAuthDownParty
        public async Task<OAuthDownParty> GetOAuthDownPartyAsync(string name)
        {
            using var response = await httpClient.GetAsync($"{await GetTenantApiUrlAsync(oauthApiUri)}?name={name}");
            var oauthDownParties = await response.ToObjectAsync<OAuthDownParty>();
            return oauthDownParties;
        }

        public async Task CreateOAuthDownPartyAsync(OAuthDownParty party)
        {
            using var response = await httpClient.PostAsJsonAsync(await GetTenantApiUrlAsync(oauthApiUri), party);
        }

        public async Task UpdateOAuthDownPartyAsync(OAuthDownParty party)
        {
            using var response = await httpClient.PutAsJsonAsync(await GetTenantApiUrlAsync(oauthApiUri), party);
        }

        public async Task DeleteOAuthDownPartyAsync(string name)
        {
            await httpClient.DeleteAsync($"{await GetTenantApiUrlAsync(oauthApiUri)}?name={name}");
        }
        #endregion

        #region SamlDownParty
        public async Task<SamlDownParty> GetSamlDownPartyAsync(string name)
        {
            using var response = await httpClient.GetAsync($"{await GetTenantApiUrlAsync(samlApiUri)}?name={name}");
            var samlDownParties = await response.ToObjectAsync<SamlDownParty>();
            return samlDownParties;
        }

        public async Task CreateSamlDownPartyAsync(SamlDownParty party)
        {
            using var response = await httpClient.PostAsJsonAsync(await GetTenantApiUrlAsync(samlApiUri), party);
        }

        public async Task UpdateSamlDownPartyAsync(SamlDownParty party)
        {
            using var response = await httpClient.PutAsJsonAsync(await GetTenantApiUrlAsync(samlApiUri), party);
        }

        public async Task DeleteSamlDownPartyAsync(string name)
        {
            await httpClient.DeleteAsync($"{await GetTenantApiUrlAsync(samlApiUri)}?name={name}");
        }
        #endregion
    }
}
