using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class UpPartyService : BaseService
    {
        private const string filterApiUri = "api/{tenant}/{track}/!filterupparty";
        private const string loginApiUri = "api/{tenant}/{track}/!loginupparty";
        private const string oidcApiUri = "api/{tenant}/{track}/!oidcupparty";
        private const string samlApiUri = "api/{tenant}/{track}/!samlupparty";
        private const string samlReadMetadataApiUri = "api/{tenant}/{track}/!samluppartyreadmetadata";

        public UpPartyService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<IEnumerable<UpParty>> FilterUpPartyAsync(string filterName) => await FilterAsync<UpParty>(filterApiUri, filterName);

        public async Task<LoginUpParty> GetLoginUpPartyAsync(string name) => await GetAsync<LoginUpParty>(loginApiUri, name);
        public async Task CreateLoginUpPartyAsync(LoginUpParty party) => await PostAsync(loginApiUri, party);
        public async Task UpdateLoginUpPartyAsync(LoginUpParty party) => await PutAsync(loginApiUri, party);
        public async Task DeleteLoginUpPartyAsync(string name) => await DeleteAsync(loginApiUri, name);

        public async Task<OidcUpParty> GetOidcUpPartyAsync(string name) => await GetAsync<OidcUpParty>(oidcApiUri, name);
        public async Task CreateOidcUpPartyAsync(OidcUpParty party) => await PostAsync(oidcApiUri, party);
        public async Task UpdateOidcUpPartyAsync(OidcUpParty party) => await PutAsync(oidcApiUri, party);
        public async Task DeleteOidcUpPartyAsync(string name) => await DeleteAsync(oidcApiUri, name);

        public async Task<SamlUpParty> GetSamlUpPartyAsync(string name) => await GetAsync<SamlUpParty>(samlApiUri, name);
        public async Task CreateSamlUpPartyAsync(SamlUpParty party) => await PostAsync(samlApiUri, party);
        public async Task UpdateSamlUpPartyAsync(SamlUpParty party) => await PutAsync(samlApiUri, party);
        public async Task DeleteSamlUpPartyAsync(string name) => await DeleteAsync(samlApiUri, name);

        public async Task<SamlUpParty> ReadSamlUpPartyMetadataAsync(SamlReadMetadataRequest metadata) => await PostResponseAsync<SamlReadMetadataRequest, SamlUpParty>(samlReadMetadataApiUri, metadata);
    }
}
