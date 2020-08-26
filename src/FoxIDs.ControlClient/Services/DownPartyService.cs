using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class DownPartyService : BaseService
    {
        private const string filterApiUri = "api/{tenant}/{track}/!filterdownparty";
        private const string oidcApiUri = "api/{tenant}/{track}/!oidcdownparty";
        private const string oidcclientsecretApiUri = "api/{tenant}/{track}/!oidcclientsecretdownparty";
        private const string oauthApiUri = "api/{tenant}/{track}/!oauthdownparty";
        private const string oauthclientsecretApiUri = "api/{tenant}/{track}/!oauthclientsecretdownparty";
        private const string samlApiUri = "api/{tenant}/{track}/!samldownparty";

        public DownPartyService(HttpClient httpClient, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClient, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<IEnumerable<DownParty>> FilterDownPartyAsync(string filterName) => await FilterAsync<DownParty>(filterApiUri, filterName);

        public async Task<OidcDownParty> GetOidcDownPartyAsync(string name) => await GetAsync<OidcDownParty>(oidcApiUri, name);
        public async Task CreateOidcDownPartyAsync(OidcDownParty party) => await PostAsync(oidcApiUri, party);
        public async Task UpdateOidcDownPartyAsync(OidcDownParty party) => await PutAsync(oidcApiUri, party);
        public async Task DeleteOidcDownPartyAsync(string name) => await DeleteAsync(oidcApiUri, name);

        public async Task<List<OAuthClientSecretResponse>> GetOidcClientSecretDownPartyAsync(string partyName) => await GetAsync<List<OAuthClientSecretResponse>>(oauthclientsecretApiUri, partyName, parmName: nameof(partyName));
        public async Task CreateOidcClientSecretDownPartyAsync(OAuthClientSecretRequest clientSecret) => await PostAsync(oidcclientsecretApiUri, clientSecret);
        public async Task DeleteOidcClientSecretDownPartyAsync(string name) => await DeleteAsync(oidcclientsecretApiUri, name);

        public async Task<OAuthDownParty> GetOAuthDownPartyAsync(string name) => await GetAsync<OAuthDownParty>(oauthApiUri, name);
        public async Task CreateOAuthDownPartyAsync(OAuthDownParty party) => await PostAsync(oauthApiUri, party);
        public async Task UpdateOAuthDownPartyAsync(OAuthDownParty party) => await PutAsync(oauthApiUri, party);
        public async Task DeleteOAuthDownPartyAsync(string name) => await DeleteAsync(oauthApiUri, name);

        public async Task<List<OAuthClientSecretResponse>> GetOAuthClientSecretDownPartyAsync(string partyName) => await GetAsync<List<OAuthClientSecretResponse>>(oauthclientsecretApiUri, partyName, parmName: nameof(partyName));
        public async Task CreateAuthClientSecretDownPartyAsync(OAuthClientSecretRequest clientSecret) => await PostAsync(oauthclientsecretApiUri, clientSecret);
        public async Task DeleteAuthClientSecretDownPartyAsync(string name) => await DeleteAsync(oauthclientsecretApiUri, name);

        public async Task<SamlDownParty> GetSamlDownPartyAsync(string name) => await GetAsync<SamlDownParty>(samlApiUri, name);
        public async Task CreateSamlDownPartyAsync(SamlDownParty party) => await PostAsync(samlApiUri, party);
        public async Task UpdateSamlDownPartyAsync(SamlDownParty party) => await PutAsync(samlApiUri, party);
        public async Task DeleteSamlDownPartyAsync(string name) => await DeleteAsync(samlApiUri, name);
    }
}
