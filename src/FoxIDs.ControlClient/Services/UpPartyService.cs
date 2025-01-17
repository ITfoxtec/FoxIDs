using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class UpPartyService : BaseService
    {
        private const string newPartyNameApiUri = "api/{tenant}/{track}/!newpartyname";
        private const string listApiUri = "api/{tenant}/{track}/!upparties";
        private const string loginApiUri = "api/{tenant}/{track}/!loginupparty";
        private const string oauthApiUri = "api/{tenant}/{track}/!oauthupparty";
        private const string oidcApiUri = "api/{tenant}/{track}/!oidcupparty";
        private const string oidcClientSecretApiUri = "api/{tenant}/{track}/!oidcclientsecretupparty";
        private const string oidcClientKeyApiUri = "api/{tenant}/{track}/!oidcclientkeyupparty";
        private const string samlApiUri = "api/{tenant}/{track}/!samlupparty";
        private const string samlReadMetadataApiUri = "api/{tenant}/{track}/!samluppartyreadmetadata";
        private const string trackLinkApiUri = "api/{tenant}/{track}/!tracklinkupparty";
        private const string externalLoginApiUri = "api/{tenant}/{track}/!externalloginupparty";
        private const string externalLoginSecretApiUri = "api/{tenant}/{track}/!externalloginsecretupparty";

        public UpPartyService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<string> GetNewPartyNameAsync() => (await GetAsync<NewPartyName>(newPartyNameApiUri, true.ToString(), parmName1: "isUpParty"))?.Name;

        public async Task<PaginationResponse<UpParty>> GetUpPartiesAsync(string filterValue, string paginationToken = null) => await GetListAsync<UpParty>(listApiUri, filterValue, parmValue2: filterValue, parmName2: "filterHrdDomains", paginationToken: paginationToken);

        public async Task<LoginUpParty> GetLoginUpPartyAsync(string name) => await GetAsync<LoginUpParty>(loginApiUri, name);
        public async Task<LoginUpParty> CreateLoginUpPartyAsync(LoginUpParty party) => await PostResponseAsync<LoginUpParty, LoginUpParty>(loginApiUri, party);
        public async Task<LoginUpParty> UpdateLoginUpPartyAsync(LoginUpParty party) => await PutResponseAsync<LoginUpParty, LoginUpParty>(loginApiUri, party);
        public async Task DeleteLoginUpPartyAsync(string name) => await DeleteAsync(loginApiUri, name);

        public async Task<OAuthUpParty> GetOAuthUpPartyAsync(string name) => await GetAsync<OAuthUpParty>(oauthApiUri, name);
        public async Task<OAuthUpParty> CreateOAuthUpPartyAsync(OAuthUpParty party) => await PostResponseAsync<OAuthUpParty, OAuthUpParty>(oauthApiUri, party);
        public async Task<OAuthUpParty> UpdateOAuthUpPartyAsync(OAuthUpParty party) => await PutResponseAsync<OAuthUpParty, OAuthUpParty>(oauthApiUri, party);
        public async Task DeleteOAuthUpPartyAsync(string name) => await DeleteAsync(oauthApiUri, name);

        public async Task<OidcUpParty> GetOidcUpPartyAsync(string name) => await GetAsync<OidcUpParty>(oidcApiUri, name);
        public async Task<OidcUpParty> CreateOidcUpPartyAsync(OidcUpParty party) => await PostResponseAsync<OidcUpParty, OidcUpParty>(oidcApiUri, party);
        public async Task<OidcUpParty> UpdateOidcUpPartyAsync(OidcUpParty party) => await PutResponseAsync<OidcUpParty, OidcUpParty>(oidcApiUri, party);
        public async Task DeleteOidcUpPartyAsync(string name) => await DeleteAsync(oidcApiUri, name);

        public async Task<OAuthClientSecretSingleResponse> GetOidcClientSecretUpPartyAsync(string partyName) => await GetAsync<OAuthClientSecretSingleResponse>(oidcClientSecretApiUri, partyName, parmName1: nameof(partyName));
        public async Task<OAuthClientSecretSingleResponse> UpdateOidcClientSecretUpPartyAsync(OAuthClientSecretSingleRequest secretRequest) => await PutResponseAsync<OAuthClientSecretSingleRequest, OAuthClientSecretSingleResponse>(oidcClientSecretApiUri, secretRequest);
        public async Task DeleteOidcClientSecretUpPartyAsync(string name) => await DeleteAsync(oidcClientSecretApiUri, name, parmName1: nameof(name));

        public async Task<OAuthClientKeyResponse> GetOidcClientKeyUpPartyAsync(string partyName) => await GetAsync<OAuthClientKeyResponse>(oidcClientKeyApiUri, partyName, parmName1: nameof(partyName));
        public async Task<OAuthClientKeyResponse> CreateOidcClientKeyUpPartyAsync(OAuthClientKeyRequest keyRequest) => await PostResponseAsync<OAuthClientKeyRequest, OAuthClientKeyResponse>(oidcClientKeyApiUri, keyRequest);
        public async Task DeleteOidcClientKeyUpPartyAsync(string name) => await DeleteAsync(oidcClientKeyApiUri, name, parmName1: nameof(name));

        public async Task<SamlUpParty> GetSamlUpPartyAsync(string name) => await GetAsync<SamlUpParty>(samlApiUri, name);
        public async Task<SamlUpParty> CreateSamlUpPartyAsync(SamlUpParty party) => await PostResponseAsync<SamlUpParty, SamlUpParty>(samlApiUri, party);
        public async Task<SamlUpParty> UpdateSamlUpPartyAsync(SamlUpParty party) => await PutResponseAsync<SamlUpParty, SamlUpParty>(samlApiUri, party);
        public async Task DeleteSamlUpPartyAsync(string name) => await DeleteAsync(samlApiUri, name);

        public async Task<SamlUpParty> ReadSamlUpPartyMetadataAsync(SamlReadMetadataRequest metadata) => await PostResponseAsync<SamlReadMetadataRequest, SamlUpParty>(samlReadMetadataApiUri, metadata);

        public async Task<TrackLinkUpParty> GetTrackLinkUpPartyAsync(string name, string trackName = null) => await GetAsync<TrackLinkUpParty>(GetApiUrl(trackLinkApiUri, trackName), name);
        public async Task<TrackLinkUpParty> CreateTrackLinkUpPartyAsync(TrackLinkUpParty party) => await PostResponseAsync<TrackLinkUpParty, TrackLinkUpParty>(trackLinkApiUri, party);
        public async Task<TrackLinkUpParty> UpdateTrackLinkUpPartyAsync(TrackLinkUpParty party) => await PutResponseAsync<TrackLinkUpParty, TrackLinkUpParty>(trackLinkApiUri, party);
        public async Task DeleteTrackLinkUpPartyAsync(string name, string trackName = null) => await DeleteAsync(GetApiUrl(trackLinkApiUri, trackName), name);

        public async Task<ExternalLoginUpParty> GetExternalLoginUpPartyAsync(string name) => await GetAsync<ExternalLoginUpParty>(externalLoginApiUri, name);
        public async Task<ExternalLoginUpParty> CreateExternalLoginUpPartyAsync(ExternalLoginUpParty party) => await PostResponseAsync<ExternalLoginUpParty, ExternalLoginUpParty>(externalLoginApiUri, party);
        public async Task<ExternalLoginUpParty> UpdateExternalLoginUpPartyAsync(ExternalLoginUpParty party) => await PutResponseAsync<ExternalLoginUpParty, ExternalLoginUpParty>(externalLoginApiUri, party);
        public async Task DeleteExternalLoginUpPartyAsync(string name) => await DeleteAsync(externalLoginApiUri, name);

        public async Task<ExternalLoginSecretResponse> GetExternalLoginSecretUpPartyAsync(string partyName) => await GetAsync<ExternalLoginSecretResponse>(externalLoginSecretApiUri, partyName, parmName1: nameof(partyName));
        public async Task<ExternalLoginSecretResponse> UpdateExternalLoginSecretUpPartyAsync(ExternalLoginSecretRequest secretRequest) => await PutResponseAsync<ExternalLoginSecretRequest, ExternalLoginSecretResponse>(externalLoginSecretApiUri, secretRequest);
        public async Task DeleteExternalLoginSecretUpPartyAsync(string name) => await DeleteAsync(externalLoginSecretApiUri, name, parmName1: nameof(name));
    }
}
