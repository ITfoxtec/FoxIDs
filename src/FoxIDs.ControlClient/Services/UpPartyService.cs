﻿using FoxIDs.Client.Logic;
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
        private const string oauthApiUri = "api/{tenant}/{track}/!oauthupparty";
        private const string oidcApiUri = "api/{tenant}/{track}/!oidcupparty";
        private const string oidcClientSecretApiUri = "api/{tenant}/{track}/!oidcclientsecretupparty";
        private const string oidcClientKeyApiUri = "api/{tenant}/{track}/!oidcclientkeyupparty";
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

        public async Task<OAuthUpParty> GetOAuthUpPartyAsync(string name) => await GetAsync<OAuthUpParty>(oauthApiUri, name);
        public async Task<OAuthUpParty> CreateOAuthUpPartyAsync(OAuthUpParty party) => await PostResponseAsync<OAuthUpParty, OAuthUpParty>(oauthApiUri, party);
        public async Task<OAuthUpParty> UpdateOAuthUpPartyAsync(OAuthUpParty party) => await PutResponseAsync<OAuthUpParty, OAuthUpParty>(oauthApiUri, party);
        public async Task DeleteOAuthUpPartyAsync(string name) => await DeleteAsync(oauthApiUri, name);

        public async Task<OidcUpParty> GetOidcUpPartyAsync(string name) => await GetAsync<OidcUpParty>(oidcApiUri, name);
        public async Task<OidcUpParty> CreateOidcUpPartyAsync(OidcUpParty party) => await PostResponseAsync<OidcUpParty, OidcUpParty>(oidcApiUri, party);
        public async Task<OidcUpParty> UpdateOidcUpPartyAsync(OidcUpParty party) => await PutResponseAsync<OidcUpParty, OidcUpParty>(oidcApiUri, party);
        public async Task DeleteOidcUpPartyAsync(string name) => await DeleteAsync(oidcApiUri, name);

        public async Task<OAuthClientSecretSingleResponse> GetOidcClientSecretUpPartyAsync(string partyName) => await GetAsync<OAuthClientSecretSingleResponse>(oidcClientSecretApiUri, partyName, parmName: nameof(partyName));
        public async Task<OAuthClientSecretSingleResponse> UpdateOidcClientSecretUpPartyAsync(OAuthClientSecretSingleRequest secretRequest) => await PutResponseAsync<OAuthClientSecretSingleRequest, OAuthClientSecretSingleResponse>(oidcClientSecretApiUri, secretRequest);
        public async Task DeleteOidcClientSecretUpPartyAsync(string name) => await DeleteAsync(oidcClientSecretApiUri, name, parmName: nameof(name));

        public async Task<OAuthClientKeyResponse> GetOidcClientKeyUpPartyAsync(string partyName) => await GetAsync<OAuthClientKeyResponse>(oidcClientKeyApiUri, partyName, parmName: nameof(partyName));
        public async Task<OAuthClientKeyResponse> CreateOidcClientKeyUpPartyAsync(OAuthClientKeyRequest keyRequest) => await PostResponseAsync<OAuthClientKeyRequest, OAuthClientKeyResponse>(oidcClientKeyApiUri, keyRequest);
        public async Task DeleteOidcClientKeyUpPartyAsync(string name) => await DeleteAsync(oidcClientKeyApiUri, name, parmName: nameof(name));

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
