using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Net.Http;
using System.Threading;
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

        public async Task<string> GetNewPartyNameAsync(CancellationToken cancellationToken = default) => (await GetAsync<NewPartyName>(newPartyNameApiUri, true.ToString(), parmName1: "isUpParty", cancellationToken: cancellationToken))?.Name;

        public async Task<PaginationResponse<UpParty>> GetUpPartiesAsync(string filterValue, string paginationToken = null, CancellationToken cancellationToken = default) => await GetListAsync<UpParty>(listApiUri, filterValue, parmValue2: filterValue, parmName2: "filterHrdDomains", paginationToken: paginationToken, cancellationToken: cancellationToken);

        public async Task<LoginUpParty> GetLoginUpPartyAsync(string name, CancellationToken cancellationToken = default) => await GetAsync<LoginUpParty>(loginApiUri, name, cancellationToken: cancellationToken);
        public async Task<LoginUpParty> CreateLoginUpPartyAsync(LoginUpParty party, CancellationToken cancellationToken = default) => await PostResponseAsync<LoginUpParty, LoginUpParty>(loginApiUri, party, cancellationToken);
        public async Task<LoginUpParty> UpdateLoginUpPartyAsync(LoginUpParty party, CancellationToken cancellationToken = default) => await PutResponseAsync<LoginUpParty, LoginUpParty>(loginApiUri, party, cancellationToken);
        public async Task DeleteLoginUpPartyAsync(string name, CancellationToken cancellationToken = default) => await DeleteAsync(loginApiUri, name, cancellationToken: cancellationToken);

        public async Task<OAuthUpParty> GetOAuthUpPartyAsync(string name, CancellationToken cancellationToken = default) => await GetAsync<OAuthUpParty>(oauthApiUri, name, cancellationToken: cancellationToken);
        public async Task<OAuthUpParty> CreateOAuthUpPartyAsync(OAuthUpParty party, CancellationToken cancellationToken = default) => await PostResponseAsync<OAuthUpParty, OAuthUpParty>(oauthApiUri, party, cancellationToken);
        public async Task<OAuthUpParty> UpdateOAuthUpPartyAsync(OAuthUpParty party, CancellationToken cancellationToken = default) => await PutResponseAsync<OAuthUpParty, OAuthUpParty>(oauthApiUri, party, cancellationToken);
        public async Task DeleteOAuthUpPartyAsync(string name, CancellationToken cancellationToken = default) => await DeleteAsync(oauthApiUri, name, cancellationToken: cancellationToken);

        public async Task<OidcUpParty> GetOidcUpPartyAsync(string name, CancellationToken cancellationToken = default) => await GetAsync<OidcUpParty>(oidcApiUri, name, cancellationToken: cancellationToken);
        public async Task<OidcUpParty> CreateOidcUpPartyAsync(OidcUpParty party, CancellationToken cancellationToken = default) => await PostResponseAsync<OidcUpParty, OidcUpParty>(oidcApiUri, party, cancellationToken);
        public async Task<OidcUpParty> UpdateOidcUpPartyAsync(OidcUpParty party, CancellationToken cancellationToken = default) => await PutResponseAsync<OidcUpParty, OidcUpParty>(oidcApiUri, party, cancellationToken);
        public async Task DeleteOidcUpPartyAsync(string name, CancellationToken cancellationToken = default) => await DeleteAsync(oidcApiUri, name, cancellationToken: cancellationToken);

        public async Task<OAuthClientSecretSingleResponse> GetOidcClientSecretUpPartyAsync(string partyName, CancellationToken cancellationToken = default) => await GetAsync<OAuthClientSecretSingleResponse>(oidcClientSecretApiUri, partyName, parmName1: nameof(partyName), cancellationToken: cancellationToken);
        public async Task<OAuthClientSecretSingleResponse> UpdateOidcClientSecretUpPartyAsync(OAuthClientSecretSingleRequest secretRequest, CancellationToken cancellationToken = default) => await PutResponseAsync<OAuthClientSecretSingleRequest, OAuthClientSecretSingleResponse>(oidcClientSecretApiUri, secretRequest, cancellationToken);
        public async Task DeleteOidcClientSecretUpPartyAsync(string name, CancellationToken cancellationToken = default) => await DeleteAsync(oidcClientSecretApiUri, name, parmName1: nameof(name), cancellationToken: cancellationToken);

        public async Task<OAuthClientKeyResponse> GetOidcClientKeyUpPartyAsync(string partyName, CancellationToken cancellationToken = default) => await GetAsync<OAuthClientKeyResponse>(oidcClientKeyApiUri, partyName, parmName1: nameof(partyName), cancellationToken: cancellationToken);
        public async Task<OAuthClientKeyResponse> CreateOidcClientKeyUpPartyAsync(OAuthClientKeyRequest keyRequest, CancellationToken cancellationToken = default) => await PostResponseAsync<OAuthClientKeyRequest, OAuthClientKeyResponse>(oidcClientKeyApiUri, keyRequest, cancellationToken);
        public async Task DeleteOidcClientKeyUpPartyAsync(string name, CancellationToken cancellationToken = default) => await DeleteAsync(oidcClientKeyApiUri, name, parmName1: nameof(name), cancellationToken: cancellationToken);

        public async Task<SamlUpParty> GetSamlUpPartyAsync(string name, CancellationToken cancellationToken = default) => await GetAsync<SamlUpParty>(samlApiUri, name, cancellationToken: cancellationToken);
        public async Task<SamlUpParty> CreateSamlUpPartyAsync(SamlUpParty party, CancellationToken cancellationToken = default) => await PostResponseAsync<SamlUpParty, SamlUpParty>(samlApiUri, party, cancellationToken);
        public async Task<SamlUpParty> UpdateSamlUpPartyAsync(SamlUpParty party, CancellationToken cancellationToken = default) => await PutResponseAsync<SamlUpParty, SamlUpParty>(samlApiUri, party, cancellationToken);
        public async Task DeleteSamlUpPartyAsync(string name, CancellationToken cancellationToken = default) => await DeleteAsync(samlApiUri, name, cancellationToken: cancellationToken);

        public async Task<SamlUpParty> ReadSamlUpPartyMetadataAsync(SamlReadMetadataRequest metadata, CancellationToken cancellationToken = default) => await PostResponseAsync<SamlReadMetadataRequest, SamlUpParty>(samlReadMetadataApiUri, metadata, cancellationToken);

        public async Task<TrackLinkUpParty> GetTrackLinkUpPartyAsync(string name, string trackName = null, CancellationToken cancellationToken = default) => await GetAsync<TrackLinkUpParty>(GetApiUrl(trackLinkApiUri, trackName), name, cancellationToken: cancellationToken);
        public async Task<TrackLinkUpParty> CreateTrackLinkUpPartyAsync(TrackLinkUpParty party, CancellationToken cancellationToken = default) => await PostResponseAsync<TrackLinkUpParty, TrackLinkUpParty>(trackLinkApiUri, party, cancellationToken);
        public async Task<TrackLinkUpParty> UpdateTrackLinkUpPartyAsync(TrackLinkUpParty party, CancellationToken cancellationToken = default) => await PutResponseAsync<TrackLinkUpParty, TrackLinkUpParty>(trackLinkApiUri, party, cancellationToken);
        public async Task DeleteTrackLinkUpPartyAsync(string name, string trackName = null, CancellationToken cancellationToken = default) => await DeleteAsync(GetApiUrl(trackLinkApiUri, trackName), name, cancellationToken: cancellationToken);

        public async Task<ExternalLoginUpParty> GetExternalLoginUpPartyAsync(string name, CancellationToken cancellationToken = default) => await GetAsync<ExternalLoginUpParty>(externalLoginApiUri, name, cancellationToken: cancellationToken);
        public async Task<ExternalLoginUpParty> CreateExternalLoginUpPartyAsync(ExternalLoginUpParty party, CancellationToken cancellationToken = default) => await PostResponseAsync<ExternalLoginUpParty, ExternalLoginUpParty>(externalLoginApiUri, party, cancellationToken);
        public async Task<ExternalLoginUpParty> UpdateExternalLoginUpPartyAsync(ExternalLoginUpParty party, CancellationToken cancellationToken = default) => await PutResponseAsync<ExternalLoginUpParty, ExternalLoginUpParty>(externalLoginApiUri, party, cancellationToken);
        public async Task DeleteExternalLoginUpPartyAsync(string name, CancellationToken cancellationToken = default) => await DeleteAsync(externalLoginApiUri, name, cancellationToken: cancellationToken);

        public async Task<ExternalLoginSecretResponse> GetExternalLoginSecretUpPartyAsync(string partyName, CancellationToken cancellationToken = default) => await GetAsync<ExternalLoginSecretResponse>(externalLoginSecretApiUri, partyName, parmName1: nameof(partyName), cancellationToken: cancellationToken);
        public async Task<ExternalLoginSecretResponse> UpdateExternalLoginSecretUpPartyAsync(ExternalLoginSecretRequest secretRequest, CancellationToken cancellationToken = default) => await PutResponseAsync<ExternalLoginSecretRequest, ExternalLoginSecretResponse>(externalLoginSecretApiUri, secretRequest, cancellationToken);
        public async Task DeleteExternalLoginSecretUpPartyAsync(string name, CancellationToken cancellationToken = default) => await DeleteAsync(externalLoginSecretApiUri, name, parmName1: nameof(name), cancellationToken: cancellationToken);
    }
}
