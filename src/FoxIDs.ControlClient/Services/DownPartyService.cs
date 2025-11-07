using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class DownPartyService : BaseService
    {
        private const string newPartyNameApiUri = "api/{tenant}/{track}/!newpartyname";
        private const string listApiUri = "api/{tenant}/{track}/!downparties";
        private const string oauthApiUri = "api/{tenant}/{track}/!oauthdownparty";
        private const string oauthclientsecretApiUri = "api/{tenant}/{track}/!oauthclientsecretdownparty";
        private const string oidcApiUri = "api/{tenant}/{track}/!oidcdownparty";
        private const string oidcclientsecretApiUri = "api/{tenant}/{track}/!oidcclientsecretdownparty";
        private const string samlApiUri = "api/{tenant}/{track}/!samldownparty";
        private const string trackLinkApiUri = "api/{tenant}/{track}/!tracklinkdownparty";

        public DownPartyService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<string> GetNewPartyNameAsync(CancellationToken cancellationToken = default) => (await GetAsync<NewPartyName>(newPartyNameApiUri, cancellationToken))?.Name;

        public async Task<PaginationResponse<DownParty>> GetDownPartiesAsync(string filterName, string paginationToken = null, CancellationToken cancellationToken = default) => await GetListAsync<DownParty>(listApiUri, filterName, paginationToken: paginationToken, cancellationToken: cancellationToken);

        public async Task<OAuthDownParty> GetOAuthDownPartyAsync(string name, CancellationToken cancellationToken = default) => await GetAsync<OAuthDownParty>(oauthApiUri, name, cancellationToken: cancellationToken);
        public async Task<OAuthDownParty> CreateOAuthDownPartyAsync(OAuthDownParty party, CancellationToken cancellationToken = default) => await PostResponseAsync<OAuthDownParty, OAuthDownParty>(oauthApiUri, party, cancellationToken);
        public async Task<OAuthDownParty> UpdateOAuthDownPartyAsync(OAuthDownParty party, CancellationToken cancellationToken = default) => await PutResponseAsync<OAuthDownParty, OAuthDownParty>(oauthApiUri, party, cancellationToken);
        public async Task DeleteOAuthDownPartyAsync(string name, CancellationToken cancellationToken = default) => await DeleteAsync(oauthApiUri, name, cancellationToken: cancellationToken);

        public async Task<List<OAuthClientSecretResponse>> GetOAuthClientSecretDownPartyAsync(string partyName, CancellationToken cancellationToken = default) => await GetAsync<List<OAuthClientSecretResponse>>(oauthclientsecretApiUri, partyName, parmName1: nameof(partyName), cancellationToken: cancellationToken);
        public async Task CreateOAuthClientSecretDownPartyAsync(OAuthClientSecretRequest clientSecret, CancellationToken cancellationToken = default) => await PostAsync(oauthclientsecretApiUri, clientSecret, cancellationToken);
        public async Task DeleteOAuthClientSecretDownPartyAsync(string name, CancellationToken cancellationToken = default) => await DeleteAsync(oauthclientsecretApiUri, name, cancellationToken: cancellationToken);

        public async Task<OidcDownParty> GetOidcDownPartyAsync(string name, CancellationToken cancellationToken = default) => await GetAsync<OidcDownParty>(oidcApiUri, name, cancellationToken: cancellationToken);
        public async Task<OidcDownParty> CreateOidcDownPartyAsync(OidcDownParty party, CancellationToken cancellationToken = default) => await PostResponseAsync<OidcDownParty, OidcDownParty>(oidcApiUri, party, cancellationToken);
        public async Task<OidcDownParty> UpdateOidcDownPartyAsync(OidcDownParty party, CancellationToken cancellationToken = default) => await PutResponseAsync<OidcDownParty, OidcDownParty>(oidcApiUri, party, cancellationToken);
        public async Task DeleteOidcDownPartyAsync(string name, CancellationToken cancellationToken = default) => await DeleteAsync(oidcApiUri, name, cancellationToken: cancellationToken);

        public async Task<List<OAuthClientSecretResponse>> GetOidcClientSecretDownPartyAsync(string partyName, CancellationToken cancellationToken = default) => await GetAsync<List<OAuthClientSecretResponse>>(oauthclientsecretApiUri, partyName, parmName1: nameof(partyName), cancellationToken: cancellationToken);
        public async Task CreateOidcClientSecretDownPartyAsync(OAuthClientSecretRequest clientSecret, CancellationToken cancellationToken = default) => await PostAsync(oidcclientsecretApiUri, clientSecret, cancellationToken);
        public async Task DeleteOidcClientSecretDownPartyAsync(string name, CancellationToken cancellationToken = default) => await DeleteAsync(oidcclientsecretApiUri, name, cancellationToken: cancellationToken);

        public async Task<SamlDownParty> GetSamlDownPartyAsync(string name, CancellationToken cancellationToken = default) => await GetAsync<SamlDownParty>(samlApiUri, name, cancellationToken: cancellationToken);
        public async Task<SamlDownParty> CreateSamlDownPartyAsync(SamlDownParty party, CancellationToken cancellationToken = default) => await PostResponseAsync<SamlDownParty, SamlDownParty>(samlApiUri, party, cancellationToken);
        public async Task<SamlDownParty> UpdateSamlDownPartyAsync(SamlDownParty party, CancellationToken cancellationToken = default) => await PutResponseAsync<SamlDownParty, SamlDownParty>(samlApiUri, party, cancellationToken);
        public async Task DeleteSamlDownPartyAsync(string name, CancellationToken cancellationToken = default) => await DeleteAsync(samlApiUri, name, cancellationToken: cancellationToken);

        public async Task<TrackLinkDownParty> GetTrackLinkDownPartyAsync(string name, string trackName = null, CancellationToken cancellationToken = default) => await GetAsync<TrackLinkDownParty>(GetApiUrl(trackLinkApiUri, trackName), name, cancellationToken: cancellationToken);
        public async Task<TrackLinkDownParty> CreateTrackLinkDownPartyAsync(TrackLinkDownParty party, string trackName = null, CancellationToken cancellationToken = default) => await PostResponseAsync<TrackLinkDownParty, TrackLinkDownParty>(GetApiUrl(trackLinkApiUri, trackName), party, cancellationToken);
        public async Task<TrackLinkDownParty> UpdateTrackLinkDownPartyAsync(TrackLinkDownParty party, CancellationToken cancellationToken = default) => await PutResponseAsync<TrackLinkDownParty, TrackLinkDownParty>(trackLinkApiUri, party, cancellationToken);
        public async Task DeleteTrackLinkDownPartyAsync(string name, string trackName = null, CancellationToken cancellationToken = default) => await DeleteAsync(GetApiUrl(trackLinkApiUri, trackName), name, cancellationToken: cancellationToken);
    }
}
