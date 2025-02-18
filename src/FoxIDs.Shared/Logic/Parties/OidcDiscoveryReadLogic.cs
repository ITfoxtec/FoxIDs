using ITfoxtec.Identity;
using ITfoxtec.Identity.Discovery;
using ITfoxtec.Identity.Models;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ITfoxtec.Identity.Util;
using System.Net.Sockets;

namespace FoxIDs.Logic
{
    public class OidcDiscoveryReadLogic
    {
        private readonly IHttpClientFactory httpClientFactory;

        public OidcDiscoveryReadLogic(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<(OidcDiscovery, JsonWebKeySet)> GetOidcDiscoveryAndValidateAsync(string authority)
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
                throw new Exception($"OIDC discovery URL '{oidcDiscoveryUrl}' error.", ex);
            }
        }

        protected async Task<OidcDiscovery> GetOidcDiscoveryAsync(string oidcDiscoveryUrl)
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient();
                using var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, oidcDiscoveryUrl));
                // Handle the response
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var result = await response.Content.ReadAsStringAsync();
                        var oidcDiscovery = result.ToObject<OidcDiscovery>();
                        return oidcDiscovery;

                    default:
                        throw new Exception($"OIDC Discovery response error, status code={response.StatusCode}.");
                }
            }
            catch (HttpRequestException hrex)
            {
                if (hrex.InnerException is SocketException soex)
                {
                    if (soex.SocketErrorCode == SocketError.TimedOut)
                    {
                        throw new Exception($"It is not possible to call the OIDC Discovery URL '{oidcDiscoveryUrl}', the call has timed out.", hrex);
                    }
                }

                throw new Exception($"It is not possible to call the OIDC Discovery URL '{oidcDiscoveryUrl}'.", hrex);
            }
            catch (TaskCanceledException tcex)
            {
                if (tcex.InnerException is TimeoutException)
                {
                    throw new Exception($"It is not possible to call the OIDC Discovery URL '{oidcDiscoveryUrl}', the call has timed out.", tcex);
                }

                throw new Exception($"It is not possible to call the OIDC Discovery URL '{oidcDiscoveryUrl}'.", tcex);
            }
            catch (Exception ex)
            {
                throw new Exception($"The call to the OIDC Discovery URL '{oidcDiscoveryUrl}' has failed.", ex);
            }
        }

        protected async Task<JsonWebKeySet> GetOidcDiscoveryKeysAsync(string jwksUri)
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient();
                using var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, jwksUri));
                // Handle the response
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var result = await response.Content.ReadAsStringAsync();
                        var jsonWebKeySet = result.ToObject<JsonWebKeySet>();
                        return jsonWebKeySet;

                    default:
                        throw new Exception($"OIDC Discovery Keys (JWKS) response error, status code={response.StatusCode}.");
                }
            }
            catch (HttpRequestException hrex)
            {
                if (hrex.InnerException is SocketException soex)
                {
                    if (soex.SocketErrorCode == SocketError.TimedOut)
                    {
                        throw new Exception($"It is not possible to call the OIDC Discovery Keys (JWKS) URL '{jwksUri}', the call has timed out.", hrex);
                    }
                }

                throw new Exception($"It is not possible to call the OIDC Discovery Keys (JWKS) URL '{jwksUri}'.", hrex);
            }
            catch (TaskCanceledException tcex)
            {
                if (tcex.InnerException is TimeoutException)
                {
                    throw new Exception($"It is not possible to call the OIDC Discovery Keys (JWKS) URL '{jwksUri}', the call has timed out.", tcex);
                }

                throw new Exception($"It is not possible to call the OIDC Discovery Keys (JWKS) URL '{jwksUri}'.", tcex);
            }
            catch (Exception ex)
            {
                throw new Exception($"The call to the OIDC Discovery Keys (JWKS) URL '{jwksUri}' has failed.", ex);
            }
        }
    }
}
