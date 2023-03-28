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
        private const string trackLinkApiUri = "api/{tenant}/{track}/!tracklinkupparty";

        public UpPartyService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<IEnumerable<UpParty>> FilterUpPartyAsync(string filterName) => await FilterAsync<UpParty>(filterApiUri, filterName);

        public async Task<LoginUpParty> GetLoginUpPartyAsync(string name) => await GetAsync<LoginUpParty>(loginApiUri, name);
        public async Task<LoginUpParty> CreateLoginUpPartyAsync(LoginUpParty party) => await PostResponseAsync<LoginUpParty, LoginUpParty>(loginApiUri, party);
        public async Task<LoginUpParty> UpdateLoginUpPartyAsync(LoginUpParty party) => await PutResponseAsync<LoginUpParty, LoginUpParty>(loginApiUri, party);
        public async Task DeleteLoginUpPartyAsync(string name) => await DeleteAsync(loginApiUri, name);

        public async Task<OidcUpParty> GetOidcUpPartyAsync(string name) => await GetAsync<OidcUpParty>(oidcApiUri, name);
        public async Task<OidcUpParty> CreateOidcUpPartyAsync(OidcUpParty party) => await PostResponseAsync<OidcUpParty, OidcUpParty>(oidcApiUri, party);
        public async Task<OidcUpParty> UpdateOidcUpPartyAsync(OidcUpParty party) => await PutResponseAsync<OidcUpParty, OidcUpParty>(oidcApiUri, party);
        public async Task DeleteOidcUpPartyAsync(string name) => await DeleteAsync(oidcApiUri, name);

        public async Task<SamlUpParty> GetSamlUpPartyAsync(string name) => await GetAsync<SamlUpParty>(samlApiUri, name);
        public async Task<SamlUpParty> CreateSamlUpPartyAsync(SamlUpParty party) => await PostResponseAsync<SamlUpParty, SamlUpParty>(samlApiUri, party);
        public async Task<SamlUpParty> UpdateSamlUpPartyAsync(SamlUpParty party) => await PutResponseAsync<SamlUpParty, SamlUpParty>(samlApiUri, party);
        public async Task DeleteSamlUpPartyAsync(string name) => await DeleteAsync(samlApiUri, name);

        public async Task<SamlUpParty> ReadSamlUpPartyMetadataAsync(SamlReadMetadataRequest metadata) => await PostResponseAsync<SamlReadMetadataRequest, SamlUpParty>(samlReadMetadataApiUri, metadata);

        public async Task<TrackLinkUpParty> GetTrackLinkUpPartyAsync(string name) => await GetAsync<TrackLinkUpParty>(trackLinkApiUri, name);
        public async Task<TrackLinkUpParty> CreateTrackLinkUpPartyAsync(TrackLinkUpParty party) => await PostResponseAsync<TrackLinkUpParty, TrackLinkUpParty>(trackLinkApiUri, party);
        public async Task<TrackLinkUpParty> UpdateTrackLinkUpPartyAsync(TrackLinkUpParty party) => await PutResponseAsync<TrackLinkUpParty, TrackLinkUpParty>(trackLinkApiUri, party);
        public async Task DeleteTrackLinkUpPartyAsync(string name) => await DeleteAsync(trackLinkApiUri, name);
    }
}
