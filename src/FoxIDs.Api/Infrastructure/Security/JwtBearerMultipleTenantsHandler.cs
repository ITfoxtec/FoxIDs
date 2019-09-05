using ITfoxtec.Identity;
using ITfoxtec.Identity.Discovery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using System;
using System.Security.Authentication;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using UrlCombineLib;
using ITfoxtec.Identity.Tokens;
using FoxIDs.Models.Config;

namespace FoxIDs.Infrastructure.Security
{
    public class JwtBearerMultipleTenantsHandler : AuthenticationHandler<JwtBearerMultipleTenantsOptions>
    {
        public const string AuthenticationScheme = "bearer-multiple-tenants";

        public JwtBearerMultipleTenantsHandler(IOptionsMonitor<JwtBearerMultipleTenantsOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            try
            {
                var accessToken = GetAccessTokenFromHeader();

                var routeBinding = Context.GetRouteBinding();
                var authority = UrlCombine.Combine(GetFoxIDsEndpoint(), routeBinding.TenantName, Constants.Routes.DefaultMasterTrackName, Options.DownParty);

                var oidcDiscoveryUri = UrlCombine.Combine(authority, IdentityConstants.OidcDiscovery.Path);
                var oidcDiscoveryHandler = Context.RequestServices.GetService<OidcDiscoveryHandler>();
                var oidcDiscovery = await oidcDiscoveryHandler.GetOidcDiscoveryAsync(oidcDiscoveryUri);
                var oidcDiscoveryKeySet = await oidcDiscoveryHandler.GetOidcDiscoveryKeysAsync(oidcDiscoveryUri);

                (var principal, var securityToken) = JwtHandler.ValidateToken(accessToken, oidcDiscovery.Issuer, oidcDiscoveryKeySet.Keys, Options.DownParty);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                return AuthenticateResult.Success(ticket);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                return AuthenticateResult.Fail(ex.Message);
            }
        }

        private string GetAccessTokenFromHeader()
        {
            string authorizationHeader = Request.Headers[HeaderNames.Authorization];
            if (authorizationHeader.IsNullOrWhiteSpace())
            {
                throw new AuthenticationException("Authorization header is empty.");
            }

            if (authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var accessToken = authorizationHeader.Substring("Bearer ".Length).Trim();

                if (accessToken.IsNullOrWhiteSpace())
                {
                    throw new AuthenticationException("Authorization header token is empty.");
                }

                return accessToken;
            }
            else
            {
                throw new AuthenticationException("Invalid Authorization header token.");
            }
        }

        private string GetFoxIDsEndpoint()
        {
            if (!Options.FoxIDsEndpoint.IsNullOrEmpty())
            {
                return Options.FoxIDsEndpoint;
            }

            var host = Context.GetHost();
            if (host.Contains("://api.", StringComparison.OrdinalIgnoreCase))
            {
                return host.Replace("://api.", "://");
            }

            if (host.Contains("api", StringComparison.OrdinalIgnoreCase))
            {
                return host.Replace("api", "", StringComparison.OrdinalIgnoreCase);
            }

            throw new InvalidConfigException("Cannot find FoxIDs api endpoint automatically from the the FoxIDs endpoint. The Settings.FoxIDsEndpoint is required.");
        }
    }
}
