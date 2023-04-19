using FoxIDs.Models;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Discovery;
using ITfoxtec.Identity.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ITfoxtec.Identity.Util;

namespace FoxIDs.Logic
{
    public class OidcDiscoveryReadLogic
    {
        private readonly IHttpClientFactory httpClientFactory;

        public OidcDiscoveryReadLogic(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        public async Task PopulateModelAsync(OidcUpParty party)
        {
            (var oidcDiscovery, var jsonWebKeySet) = await GetOidcDiscoveryAndValidateAsync(party.Authority);

            party.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (party.EditIssuersInAutomatic != true || string.IsNullOrWhiteSpace(party.Issuers?.FirstOrDefault()))
            {
                party.Issuers = new List<string> { oidcDiscovery.Issuer };
            }
            party.Client.AuthorizeUrl = oidcDiscovery.AuthorizationEndpoint;
            if (!oidcDiscovery.TokenEndpoint.IsNullOrEmpty())
            {
                party.Client.TokenUrl = oidcDiscovery.TokenEndpoint;                
            }
            else if (party.Client.ResponseType?.Contains(IdentityConstants.ResponseTypes.Code) == true)
            {
                party.Client.ResponseType = $"{IdentityConstants.ResponseTypes.Token} {IdentityConstants.ResponseTypes.IdToken}";
                party.Client.EnablePkce = false;
            }
            if (!oidcDiscovery.UserInfoEndpoint.IsNullOrEmpty())
            {
                party.Client.UserInfoUrl = oidcDiscovery.UserInfoEndpoint;
            }
            if (!oidcDiscovery.EndSessionEndpoint.IsNullOrEmpty())
            {
                party.Client.EndSessionUrl = oidcDiscovery.EndSessionEndpoint;
            }
            party.Keys = jsonWebKeySet.Keys?.ToList();
        }

        private async Task<(OidcDiscovery, JsonWebKeySet)> GetOidcDiscoveryAndValidateAsync(string authority)
        {
            var oidcDiscoveryUrl = UrlCombine.Combine(authority, IdentityConstants.OidcDiscovery.Path);
            try
            {
                var oidcDiscovery = await GetOidcDiscoveryAsync(oidcDiscoveryUrl);
                if (oidcDiscovery.Issuer.IsNullOrEmpty())
                {
                    throw new Exception($"{nameof(oidcDiscovery.Issuer)} is required.");
                }
                if (oidcDiscovery.AuthorizationEndpoint.IsNullOrEmpty())
                {
                    throw new Exception($"{nameof(oidcDiscovery.AuthorizationEndpoint)} is required.");
                }
                if (oidcDiscovery.JwksUri.IsNullOrEmpty())
                {
                    throw new Exception($"{nameof(oidcDiscovery.JwksUri)} is required.");
                }

                var jsonWebKeySet = await GetOidcDiscoveryKeysAsync(oidcDiscovery.JwksUri);
                if (jsonWebKeySet.Keys?.Count <= 0)
                {
                    throw new Exception($"At least one key in {nameof(jsonWebKeySet.Keys)} is required.");
                }

                return (oidcDiscovery, jsonWebKeySet);
            }
            catch (Exception ex)
            {
                throw new Exception($"OIDC discovery error for OIDC discovery URL '{oidcDiscoveryUrl}'.", ex);
            }
        }

        protected async Task<OidcDiscovery> GetOidcDiscoveryAsync(string oidcDiscoveryUrl)
        {
            var httpClient = httpClientFactory.CreateClient(nameof(HttpClient));
            using var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, oidcDiscoveryUrl));
            // Handle the response
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    var result = await response.Content.ReadAsStringAsync();
                    var oidcDiscovery = result.ToObject<OidcDiscovery>();
                    return oidcDiscovery;

                default:
                    throw new Exception($"Status Code OK expected. Unable to read OIDC Discovery '{oidcDiscoveryUrl}'. StatusCode={response.StatusCode}..");
            }
        }

        protected async Task<JsonWebKeySet> GetOidcDiscoveryKeysAsync(string jwksUri)
        {
            var httpClient = httpClientFactory.CreateClient(nameof(HttpClient));
            using var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, jwksUri));
            // Handle the response
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    var result = await response.Content.ReadAsStringAsync();
                    var jsonWebKeySet = result.ToObject<JsonWebKeySet>();
                    return jsonWebKeySet;

                default:
                    throw new Exception($"Status Code OK expected. StatusCode={response.StatusCode}.");
            }
        }
    }
}
