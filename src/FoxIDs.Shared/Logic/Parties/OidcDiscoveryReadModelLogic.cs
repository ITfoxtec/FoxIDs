using FoxIDs.Models;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Discovery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class OidcDiscoveryReadModelLogic<MParty, MClient> : OidcDiscoveryReadLogic where MParty : OAuthUpParty<MClient> where MClient : OAuthUpClient
    {
        public OidcDiscoveryReadModelLogic(IHttpClientFactory httpClientFactory) : base(httpClientFactory) 
        { }

        public async Task<MParty> PopulateModelAsync(MParty party)
        {
            (var oidcDiscovery, var jsonWebKeySet) = await GetOidcDiscoveryAndValidateAsync(party.Authority);

            party.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (party.EditIssuersInAutomatic != true || string.IsNullOrWhiteSpace(party.Issuers?.FirstOrDefault()))
            {
                party.Issuers = new List<string> { oidcDiscovery.Issuer };
            }
            party.Client.AuthorizeUrl = oidcDiscovery.AuthorizationEndpoint;

            if (party.Authority.StartsWith("https://facebook.com", StringComparison.OrdinalIgnoreCase) 
                || party.Authority.StartsWith("https://www.facebook.com", StringComparison.OrdinalIgnoreCase)
                || party.Authority.StartsWith("https://limited.facebook.com", StringComparison.OrdinalIgnoreCase))
            {
                CorrectFacebookOidcDiscoveryAddTokenEndpoint(oidcDiscovery);
            }

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
            if(party.Keys?.Count() > Constants.Models.OAuthUpParty.KeysWithX509InfoMax)
            {
                foreach (var key in party.Keys)
                {
                    key.X5u = null;
                    key.X5c = null;
                    key.X5t = null;
                    key.X5tS256 = null;
                }
            }

            return party;
        }

        private void CorrectFacebookOidcDiscoveryAddTokenEndpoint(OidcDiscovery oidcDiscovery)
        {
            oidcDiscovery.TokenEndpoint = "https://graph.facebook.com/v2.8/oauth/access_token";
        }
    }
}
